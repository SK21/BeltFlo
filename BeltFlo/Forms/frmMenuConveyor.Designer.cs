using BeltFlo.Language;

namespace BeltFlo.Forms
{
    partial class frmMenuConveyor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlTitle        = new System.Windows.Forms.Panel();
            this.lblTitle        = new System.Windows.Forms.Label();
            this.pnlContent      = new System.Windows.Forms.Panel();
            this.lblPulse        = new System.Windows.Forms.Label();
            this.lblPulseVal     = new System.Windows.Forms.Label();
            this.lblPulseUnit    = new System.Windows.Forms.Label();
            this.lblPpr          = new System.Windows.Forms.Label();
            this.lblPprVal       = new System.Windows.Forms.Label();
            this.lblSection      = new System.Windows.Forms.Label();
            this.lblSectionVal   = new System.Windows.Forms.Label();
            this.lblSectionUnit  = new System.Windows.Forms.Label();
            this.lblMinFlow      = new System.Windows.Forms.Label();
            this.lblMinFlowVal   = new System.Windows.Forms.Label();
            this.lblMinFlowUnit  = new System.Windows.Forms.Label();
            this.lblStop         = new System.Windows.Forms.Label();
            this.lblStopVal      = new System.Windows.Forms.Label();
            this.lblStopUnit     = new System.Windows.Forms.Label();
            this.lblDelay        = new System.Windows.Forms.Label();
            this.lblDelayVal     = new System.Windows.Forms.Label();
            this.lblDelayUnit    = new System.Windows.Forms.Label();
            this.lblLivePulses   = new System.Windows.Forms.Label();
            this.lblLiveRate     = new System.Windows.Forms.Label();
            this.lblLiveBelt     = new System.Windows.Forms.Label();
            this.lblLiveDistance = new System.Windows.Forms.Label();
            this.lblLiveState    = new System.Windows.Forms.Label();
            this.btnMeasure      = new System.Windows.Forms.Button();
            this.btnResetDist    = new System.Windows.Forms.Button();
            this.lblMeasure      = new System.Windows.Forms.Label();
            this.btnSave         = new System.Windows.Forms.Button();
            this.btnClose        = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // ── Title bar ────────────────────────────────────────────────────
            this.pnlTitle.Dock   = System.Windows.Forms.DockStyle.Top;
            this.pnlTitle.Height = 48;

            this.lblTitle.Text      = Lang.lgTitleConveyor;
            this.lblTitle.Font      = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(180, 200, 220);
            this.lblTitle.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.AutoSize  = false;
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.pnlTitle.Controls.Add(this.lblTitle);

            // ── Content ───────────────────────────────────────────────────────
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;

            var lf = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);
            var vf = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            var sf = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            var bf = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);

            // Settings. Numbers are plain boxes that open the numpad — no spinners.
            SetRow(this.lblPulse,   Lang.lgBeltPerPulse,     this.lblPulseVal,   this.lblPulseUnit,   8,   lf, vf);
            SetRow(this.lblPpr,     Lang.lgPulsesPerRev,     this.lblPprVal,     null,                48,  lf, vf);
            SetRow(this.lblSection, Lang.lgSectionLength,    this.lblSectionVal, this.lblSectionUnit, 88,  lf, vf);
            SetRow(this.lblMinFlow, Lang.lgEmptyBeltBelow,   this.lblMinFlowVal, this.lblMinFlowUnit, 128, lf, vf);
            SetRow(this.lblStop,    Lang.lgBeltStoppedAfter, this.lblStopVal,    this.lblStopUnit,    168, lf, vf);
            SetRow(this.lblDelay,   Lang.lgDigToScaleDelay,  this.lblDelayVal,   this.lblDelayUnit,   208, lf, vf);
            this.lblStopUnit.Text  = "s";
            this.lblDelayUnit.Text = "s";

            this.lblPulseVal.Click   += new System.EventHandler(this.lblPulseVal_Click);
            this.lblPprVal.Click     += new System.EventHandler(this.lblPprVal_Click);
            this.lblSectionVal.Click += new System.EventHandler(this.lblSectionVal_Click);
            this.lblMinFlowVal.Click += new System.EventHandler(this.lblMinFlowVal_Click);
            this.lblStopVal.Click    += new System.EventHandler(this.lblStopVal_Click);
            this.lblDelayVal.Click   += new System.EventHandler(this.lblDelayVal_Click);

            // Live readout from the module
            SetLive(this.lblLivePulses,   8,   254, sf);
            SetLive(this.lblLiveRate,     284, 254, sf);
            SetLive(this.lblLiveBelt,     8,   280, sf);
            SetLive(this.lblLiveDistance, 284, 280, sf);
            SetLive(this.lblLiveState,    8,   306, sf);

            // Measure Belt / Reset Distance
            this.btnMeasure.Text      = Lang.lgMeasureBelt; this.btnMeasure.Font = bf; this.btnMeasure.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMeasure.Location  = new System.Drawing.Point(8, 338); this.btnMeasure.Size = new System.Drawing.Size(200, 44);
            this.btnMeasure.Click    += new System.EventHandler(this.btnMeasure_Click);

            this.btnResetDist.Text      = Lang.lgResetDistance; this.btnResetDist.Font = bf; this.btnResetDist.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetDist.Location  = new System.Drawing.Point(216, 338); this.btnResetDist.Size = new System.Drawing.Size(200, 44);
            this.btnResetDist.Click    += new System.EventHandler(this.btnResetDist_Click);

            this.lblMeasure.Font      = sf;
            this.lblMeasure.Location  = new System.Drawing.Point(8, 388); this.lblMeasure.AutoSize = false; this.lblMeasure.Size = new System.Drawing.Size(548, 34);
            this.lblMeasure.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Save / Close
            this.btnSave.Text      = Lang.lgSave; this.btnSave.Font = bf; this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Location  = new System.Drawing.Point(8, 428); this.btnSave.Size = new System.Drawing.Size(130, 44);
            this.btnSave.Click    += new System.EventHandler(this.btnSave_Click);

            this.btnClose.Text      = Lang.lgClose; this.btnClose.Font = bf; this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Location  = new System.Drawing.Point(422, 428); this.btnClose.Size = new System.Drawing.Size(130, 44);
            this.btnClose.Click    += new System.EventHandler(this.btnClose_Click);

            this.pnlContent.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblPulse, lblPulseVal, lblPulseUnit,
                lblPpr, lblPprVal,
                lblSection, lblSectionVal, lblSectionUnit,
                lblMinFlow, lblMinFlowVal, lblMinFlowUnit,
                lblStop, lblStopVal, lblStopUnit,
                lblDelay, lblDelayVal, lblDelayUnit,
                lblLivePulses, lblLiveRate, lblLiveBelt, lblLiveDistance, lblLiveState,
                btnMeasure, btnResetDist, lblMeasure,
                btnSave, btnClose });

            // ── Form ──────────────────────────────────────────────────────────
            this.ClientSize      = new System.Drawing.Size(564, 532);
            this.MinimumSize     = new System.Drawing.Size(564, 532);
            this.MaximumSize     = new System.Drawing.Size(564, 532);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Padding         = new System.Windows.Forms.Padding(2);
            this.BackColor       = System.Drawing.Color.White;
            this.TopMost         = true;
            this.ShowInTaskbar   = false;
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Font            = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.Name            = "frmMenuConveyor";
            this.Text            = "Conveyor Setup";
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlTitle);
            this.Load += new System.EventHandler(this.frmMenuConveyor_Load);

            this.ResumeLayout(false);
        }

        // One settings row: label, tap-to-edit value box, optional unit.
        private static void SetRow(System.Windows.Forms.Label label, string text,
                                   System.Windows.Forms.Label value, System.Windows.Forms.Label unit,
                                   int y, System.Drawing.Font lf, System.Drawing.Font vf)
        {
            label.Text      = text; label.Font = lf; label.AutoSize = false;
            label.Location  = new System.Drawing.Point(8, y); label.Size = new System.Drawing.Size(220, 32);
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            value.Font        = vf; value.AutoSize = false;
            value.Location    = new System.Drawing.Point(236, y); value.Size = new System.Drawing.Size(120, 32);
            value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            value.TextAlign   = System.Drawing.ContentAlignment.MiddleCenter;
            value.Cursor      = System.Windows.Forms.Cursors.Hand;

            if (unit != null)
            {
                unit.Font      = vf; unit.AutoSize = false;
                unit.Location  = new System.Drawing.Point(364, y); unit.Size = new System.Drawing.Size(180, 32);
                unit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }
        }

        private static void SetLive(System.Windows.Forms.Label label, int x, int y, System.Drawing.Font f)
        {
            label.Font      = f; label.AutoSize = false;
            label.Location  = new System.Drawing.Point(x, y); label.Size = new System.Drawing.Size(270, 24);
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        }

        private System.Windows.Forms.Panel  pnlTitle;
        private System.Windows.Forms.Label  lblTitle;
        private System.Windows.Forms.Panel  pnlContent;
        private System.Windows.Forms.Label  lblPulse;
        private System.Windows.Forms.Label  lblPulseVal;
        private System.Windows.Forms.Label  lblPulseUnit;
        private System.Windows.Forms.Label  lblPpr;
        private System.Windows.Forms.Label  lblPprVal;
        private System.Windows.Forms.Label  lblSection;
        private System.Windows.Forms.Label  lblSectionVal;
        private System.Windows.Forms.Label  lblSectionUnit;
        private System.Windows.Forms.Label  lblMinFlow;
        private System.Windows.Forms.Label  lblMinFlowVal;
        private System.Windows.Forms.Label  lblMinFlowUnit;
        private System.Windows.Forms.Label  lblStop;
        private System.Windows.Forms.Label  lblStopVal;
        private System.Windows.Forms.Label  lblStopUnit;
        private System.Windows.Forms.Label  lblDelay;
        private System.Windows.Forms.Label  lblDelayVal;
        private System.Windows.Forms.Label  lblDelayUnit;
        private System.Windows.Forms.Label  lblLivePulses;
        private System.Windows.Forms.Label  lblLiveRate;
        private System.Windows.Forms.Label  lblLiveBelt;
        private System.Windows.Forms.Label  lblLiveDistance;
        private System.Windows.Forms.Label  lblLiveState;
        private System.Windows.Forms.Button btnMeasure;
        private System.Windows.Forms.Button btnResetDist;
        private System.Windows.Forms.Label  lblMeasure;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;
    }
}
