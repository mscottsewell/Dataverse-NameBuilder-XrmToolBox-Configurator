using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace NameBuilderConfigurator
{
    internal sealed class PluginAssemblyInstaller
    {
        private readonly IOrganizationService service;

        // Lazy-loaded metadata for the packaged NameBuilder assembly (hash + public key token)
        private static readonly Lazy<(string Hash, string PublicKeyToken)> ExpectedAssemblyMetadata =
            new Lazy<(string Hash, string PublicKeyToken)>(LoadExpectedAssemblyMetadata, isThreadSafe: true);

        public PluginAssemblyInstaller(IOrganizationService organizationService)
        {
            service = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        public PluginAssemblyInstallResult InstallOrUpdate(string assemblyPath)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("The selected plug-in assembly could not be found.", assemblyPath);
            }

            var absolutePath = Path.GetFullPath(assemblyPath);
            var assemblyName = AssemblyName.GetAssemblyName(absolutePath);
            var shortName = assemblyName?.Name ?? Path.GetFileNameWithoutExtension(absolutePath);
            var version = assemblyName?.Version?.ToString() ?? "1.0.0.0";
            var culture = string.IsNullOrWhiteSpace(assemblyName?.CultureName) ? "neutral" : assemblyName.CultureName;
            var publicKeyToken = FormatPublicKeyToken(assemblyName?.GetPublicKeyToken());
            var rawBytes = File.ReadAllBytes(absolutePath);
            var content = Convert.ToBase64String(rawBytes);
            var fileHash = ComputeSha256Hex(rawBytes);

            var existingAssembly = FindExistingAssembly(shortName);
            Guid assemblyId;
            var createdAssembly = false;

            if (existingAssembly == null)
            {
                var entity = new Entity("pluginassembly")
                {
                    ["name"] = shortName,
                    ["culture"] = culture,
                    ["version"] = version,
                    ["introducedversion"] = version,
                    ["isolationmode"] = new OptionSetValue(2),
                    ["sourcetype"] = new OptionSetValue(0),
                    ["content"] = content,
                    ["description"] = "NameBuilder plug-in assembly (installed via NameBuilder Configurator)"
                };

                if (!string.IsNullOrWhiteSpace(publicKeyToken))
                {
                    entity["publickeytoken"] = publicKeyToken;
                }

                assemblyId = service.Create(entity);
                createdAssembly = true;
            }
            else
            {
                assemblyId = existingAssembly.Id;
                var update = new Entity("pluginassembly") { Id = assemblyId };
                update["content"] = content;
                update["version"] = version;
                update["introducedversion"] = version;
                update["culture"] = culture;
                if (!string.IsNullOrWhiteSpace(publicKeyToken))
                {
                    update["publickeytoken"] = publicKeyToken;
                }

                service.Update(update);
            }

            var pluginClasses = DiscoverPluginClasses(absolutePath);
            var pluginTypeResult = EnsurePluginTypes(assemblyId, pluginClasses);

            return new PluginAssemblyInstallResult
            {
                AssemblyId = assemblyId,
                AssemblyName = shortName,
                Version = version,
                CreatedAssembly = createdAssembly,
                CreatedPluginTypes = pluginTypeResult.Created,
                ExistingPluginTypes = pluginTypeResult.Existing,
                PluginTypeNames = pluginClasses.Select(c => c.TypeName).ToList(),
                AssemblyPath = absolutePath,
                AssemblyHash = fileHash
            };
        }

        private Entity FindExistingAssembly(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                assemblyName = "NameBuilder";
            }

            var query = new QueryExpression("pluginassembly")
            {
                ColumnSet = new ColumnSet("pluginassemblyid", "name", "version", "modifiedon"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, assemblyName)
                    }
                }
            };

            return service.RetrieveMultiple(query).Entities.FirstOrDefault();
        }

        private static string FormatPublicKeyToken(byte[] token)
        {
            if (token == null || token.Length == 0)
            {
                return null;
            }

            return BitConverter.ToString(token).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string ComputeSha256Hex(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(data);
                var sb = new System.Text.StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private IReadOnlyList<PluginClassDefinition> DiscoverPluginClasses(string absolutePath)
        {
            try
            {
                // Validate assembly name before loading to ensure it's the expected plugin
                var assemblyName = AssemblyName.GetAssemblyName(absolutePath);
                ValidateExpectedAssembly(assemblyName, absolutePath);

                var assembly = Assembly.LoadFrom(absolutePath);
                var pluginContract = typeof(IPlugin);
                var definitions = new List<PluginClassDefinition>();

                foreach (var type in assembly.GetTypes())
                {
                    if (type == null || type.IsAbstract || !type.IsClass)
                    {
                        continue;
                    }

                    if (!pluginContract.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    var friendlyName = type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
                    if (string.IsNullOrWhiteSpace(friendlyName))
                    {
                        friendlyName = type.Name;
                    }

                    var description = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.FullName;

                    definitions.Add(new PluginClassDefinition
                    {
                        TypeName = type.FullName,
                        FriendlyName = friendlyName,
                        Description = description
                    });
                }

                if (definitions.Count == 0)
                {
                    throw new InvalidOperationException("No plug-in classes implementing IPlugin were found in the selected assembly.");
                }

                return definitions;
            }
            catch (ReflectionTypeLoadException ex)
            {
                var loaderDetails = string.Join(" | ", ex.LoaderExceptions.Select(e => e?.Message).Where(m => !string.IsNullOrWhiteSpace(m)));
                throw new InvalidOperationException($"Unable to inspect the plug-in assembly: {loaderDetails}", ex);
            }
        }

        private static void ValidateExpectedAssembly(AssemblyName assemblyName, string assemblyPath)
        {
            if (assemblyName == null)
            {
                throw new InvalidOperationException("Unable to read assembly metadata from the selected file. Verify that it is a valid .NET assembly.");
            }

            // Accept both exact name match and files that match the expected name
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
            var asmName = assemblyName.Name;

            if (!asmName.Equals("NameBuilder", StringComparison.OrdinalIgnoreCase) &&
                !fileName.Equals("NameBuilder", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The selected DLL does not appear to be the NameBuilder assembly. " +
                    $"Expected assembly name 'NameBuilder' but found '{asmName}'.");
            }

            // Compare against packaged metadata if available (public key token + hash)
            var expected = ExpectedAssemblyMetadata.Value;

            if (!string.IsNullOrWhiteSpace(expected.PublicKeyToken))
            {
                var candidateToken = FormatPublicKeyToken(assemblyName.GetPublicKeyToken());
                if (string.IsNullOrWhiteSpace(candidateToken) ||
                    !candidateToken.Equals(expected.PublicKeyToken, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The selected DLL does not match the signed NameBuilder assembly (public key token mismatch).");
                }
            }

            if (!string.IsNullOrWhiteSpace(expected.Hash))
            {
                var currentHash = ComputeSha256Hex(File.ReadAllBytes(assemblyPath));
                if (string.IsNullOrWhiteSpace(currentHash) ||
                    !currentHash.Equals(expected.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The selected DLL does not match the packaged NameBuilder build (hash mismatch).");
                }
            }
        }

        private static (string Hash, string PublicKeyToken) LoadExpectedAssemblyMetadata()
        {
            try
            {
                var bundledPath = ResolveBundledAssemblyPath();
                if (string.IsNullOrWhiteSpace(bundledPath) || !File.Exists(bundledPath))
                {
                    return (Hash: null, PublicKeyToken: null);
                }

                var bytes = File.ReadAllBytes(bundledPath);
                var hash = ComputeSha256Hex(bytes);
                var name = AssemblyName.GetAssemblyName(bundledPath);
                var pkt = FormatPublicKeyToken(name?.GetPublicKeyToken());
                return (Hash: hash, PublicKeyToken: pkt);
            }
            catch
            {
                // Fallback to name-only validation if packaged metadata cannot be read
                return (Hash: null, PublicKeyToken: null);
            }
        }

        private static string ResolveBundledAssemblyPath()
        {
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var baseDir = string.IsNullOrWhiteSpace(assemblyLocation)
                    ? null
                    : Path.GetDirectoryName(assemblyLocation);

                var candidates = new List<string>();
                if (!string.IsNullOrWhiteSpace(baseDir))
                {
                    candidates.Add(Path.Combine(baseDir, "Assets", "DataversePlugin", "NameBuilder.dll"));

                    var parent = Directory.GetParent(baseDir);
                    if (parent != null)
                    {
                        candidates.Add(Path.Combine(parent.FullName, "Assets", "DataversePlugin", "NameBuilder.dll"));
                    }
                }

                foreach (var candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
            catch
            {
                // Ignore resolution failures and allow fallback validation
            }

            return null;
        }

        private (int Created, int Existing) EnsurePluginTypes(Guid assemblyId, IReadOnlyList<PluginClassDefinition> pluginClasses)
        {
            var query = new QueryExpression("plugintype")
            {
                ColumnSet = new ColumnSet("plugintypeid", "typename"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("pluginassemblyid", ConditionOperator.Equal, assemblyId)
                    }
                }
            };

            var existingTypes = service.RetrieveMultiple(query).Entities;
            var existingMap = existingTypes
                .Where(e => !string.IsNullOrWhiteSpace(e.GetAttributeValue<string>("typename")))
                .ToDictionary(e => e.GetAttributeValue<string>("typename"), e => e, StringComparer.OrdinalIgnoreCase);

            var createdCount = 0;
            var existingCount = 0;

            foreach (var pluginClass in pluginClasses)
            {
                if (existingMap.ContainsKey(pluginClass.TypeName))
                {
                    existingCount++;
                    continue;
                }

                var pluginTypeEntity = new Entity("plugintype")
                {
                    ["typename"] = pluginClass.TypeName,
                    ["friendlyname"] = pluginClass.FriendlyName,
                    ["name"] = pluginClass.FriendlyName,
                    ["description"] = pluginClass.Description,
                    ["pluginassemblyid"] = new EntityReference("pluginassembly", assemblyId)
                };

                service.Create(pluginTypeEntity);
                createdCount++;
            }

            return (createdCount, existingCount);
        }

        private sealed class PluginClassDefinition
        {
            public string TypeName { get; set; }
            public string FriendlyName { get; set; }
            public string Description { get; set; }
        }
    }

    internal sealed class PluginAssemblyInstallResult
    {
        public Guid AssemblyId { get; set; }
        public string AssemblyName { get; set; }
        public string Version { get; set; }
        public bool CreatedAssembly { get; set; }
        public int CreatedPluginTypes { get; set; }
        public int ExistingPluginTypes { get; set; }
        public IReadOnlyList<string> PluginTypeNames { get; set; } = Array.Empty<string>();
        public string AssemblyPath { get; set; }
        public string AssemblyHash { get; set; }
    }
}
