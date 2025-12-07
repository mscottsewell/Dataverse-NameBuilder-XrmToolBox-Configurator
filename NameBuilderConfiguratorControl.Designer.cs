namespace  NameBuilderConfigurator
{
    partial class  NameBuilderConfiguratorControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // Cached fonts for proper resource management
        private System.Drawing.Font _fontSegoeUI8;
        private System.Drawing.Font _fontSegoeUI85;
        private System.Drawing.Font _fontSegoeUI9;
        private System.Drawing.Font _fontSegoeUI10;
        private System.Drawing.Font _fontSegoeUI10Bold;
        private System.Drawing.Font _fontSegoeUI11;
        private System.Drawing.Font _fontSegoeUI11Bold;
        private System.Drawing.Font _fontSegoeUI12Bold;
        private System.Drawing.Font _fontSegoeUI75;
        private System.Drawing.Font _fontSegoeUI10Italic;
        private System.Drawing.Font _fontConsolas9;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            // Dispose all cached fonts and other resources
            if (disposing)
            {
                _fontSegoeUI8?.Dispose();
                _fontSegoeUI85?.Dispose();
                _fontSegoeUI9?.Dispose();
                _fontSegoeUI10?.Dispose();
                _fontSegoeUI10Bold?.Dispose();
                _fontSegoeUI11?.Dispose();
                _fontSegoeUI11Bold?.Dispose();
                _fontSegoeUI12Bold?.Dispose();
                _fontSegoeUI75?.Dispose();
                _fontSegoeUI10Italic?.Dispose();
                _fontConsolas9?.Dispose();
                helpToolTip?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
