namespace SensMaster
{
    partial class FixedReaderService
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.eventLogger = new System.Diagnostics.EventLog();
            this.pmsTimer = new System.Windows.Forms.Timer(this.components);
            this.DBtimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.eventLogger)).BeginInit();
            // 
            // pmsTimer
            // 
            this.pmsTimer.Tick += new System.EventHandler(this.pmsTimer_Tick);
            // 
            // DBtimer
            // 
            this.DBtimer.Tick += new System.EventHandler(this.DBtimer_Tick);
            // 
            // FixedReaderService
            // 
            this.ServiceName = "Service1";
            ((System.ComponentModel.ISupportInitialize)(this.eventLogger)).EndInit();

        }

        #endregion

        private System.Diagnostics.EventLog eventLogger;
        private System.Windows.Forms.Timer pmsTimer;
        private System.Windows.Forms.Timer DBtimer;
    }
}
