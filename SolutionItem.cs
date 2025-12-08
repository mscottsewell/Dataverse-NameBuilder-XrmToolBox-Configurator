using System;

namespace NameBuilderConfigurator
{
    public class SolutionItem
    {
        public Guid SolutionId { get; set; }
        public string FriendlyName { get; set; }
        public string UniqueName { get; set; }
        public bool IsManaged { get; set; }

        public override string ToString() => string.IsNullOrWhiteSpace(FriendlyName) ? UniqueName : FriendlyName;
    }
}
