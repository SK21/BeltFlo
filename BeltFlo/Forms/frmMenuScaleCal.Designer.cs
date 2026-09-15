using BeltFlo.Language;

namespace BeltFlo.Forms
{
    partial class frmMenuScaleCal
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlTitle      = new System.Windows.Forms.Panel();
            this.lblTitle      = new System.Windows.Forms.Label();
            this.pnlContent    = new System.Windows.Forms.Panel();
            this.lblRaw        = new System.Windows.Forms.Label();
            this.lblRawVal     = new System.Windows.Forms.Label();
            this.lblWeight     = new System.Windows.Forms.Label();
            this.lblWeightVal  = new System.Windows.Forms.Label();
            this.lblStable     = new System.Windows.Forms.Label();
            this.lblStableVal  = new System.Windows.Forms.Label();
            this.lblZero       = new System.Windows.Forms.Label();
            this.lblZeroVal    = new System.Windows.Forms.Label();
            this.lblZeroInfo   = new System.Windows.Forms.Label();
            this.lblSpan       = new System.Windows.Forms.Label();
            this.lblSpanVal    = new System.Windows.Forms.Label();
            this.lblSpanInfo   = new System.Windows.Forms.Label();
            this.btnZero       = new System.Windows.Forms.Button();
            this.btnWeight     = new System.Windows.Forms.Button();
            this.lblStatus     = new System.Windows.Forms.Label();
            this.btnSave       = new System.Windows.Forms.Button();
            this.btnClose      = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // ── Title bar ────────────────────────────────────────────────────
            this.pnlTitle.Dock   = System.Windows.Forms.DockStyle.Top;
            this.pnlTitle.Height = 48;

            this.lblTitle.Text      = Lang.lgTitleScaleCal;
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

            // Live readout
            SetRow(this.lblRaw,    Lang.lgCalRaw,     this.lblRawVal,    null, 8,   lf, vf, sf);
            SetRow(this.lblWeight, Lang.lgCalWeight,  this.lblWeightVal, null, 46,  lf, vf, sf);
            SetRow(this.lblStable, Lang.lgCalReading, this.lblStableVal, null, 84,  lf, vf, sf);

            // Calibration
            SetRow(this.lblZero,   Lang.lgCalZero,    this.lblZeroVal,   this.lblZeroInfo, 132, lf, vf, sf);
            SetRow(this.lblSpan,   Lang.lgCalSpan,    this.lblSpanVal,   this.lblSpanInfo, 170, lf, vf, sf);

            this.btnZero.Text      = Lang.lgCalZeroScale; this.btnZero.Font = bf; this.btnZero.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnZero.Location  = new System.Drawing.Point(8, 220); this.btnZero.Size = new System.Drawing.Size(200, 44);
            this.btnZero.Click    += new System.EventHandler(this.btnZero_Click);

            this.btnWeight.Text      = Lang.lgCalKnownWeight; this.btnWeight.Font = bf; this.btnWeight.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnWeight.Location  = new System.Drawing.Point(216, 220); this.btnWeight.Size = new System.Drawing.Size(200, 44);
            this.btnWeight.Click    += new System.EventHandler(this.btnWeight_Click);

            this.lblStatus.Font      = sf;
            this.lblStatus.Location  = new System.Drawing.Point(8, 270); this.lblStatus.AutoSize = false; this.lblStatus.Size = new System.Drawing.Size(548, 48);
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.btnSave.Text      = Lang.lgSave; this.btnSave.Font = bf; this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Location  = new System.Drawing.Point(8, 328); this.btnSave.Size = new System.Drawing.Size(130, 44);
            this.btnSave.Click    += new System.EventHandler(this.btnSave_Click);

            this.btnClose.Text      = Lang.lgClose; this.btnClose.Font = bf; this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Location  = new System.Drawing.Point(422, 328); this.btnClose.Size = new System.Drawing.Size(130, 44);
            this.btnClose.Click    += new System.EventHandler(this.btnClose_Click);

            this.pnlContent.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblRaw, lblRawVal, lblWeight, lblWeightVal, lblStable, lblStableVal,
                lblZero, lblZeroVal, lblZeroInfo, lblSpan, lblSpanVal, lblSpanInfo,
                btnZero, btnWeight, lblStatus, btnSave, btnClose });

            // ── Form ──────────────────────────────────────────────────────────
            this.ClientSize      = new System.Drawing.Size(564, 432);
            this.MinimumSize     = new System.Drawing.Size(564, 432);
            this.MaximumSize     = new System.Drawing.Size(564, 432);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Padding         = new System.Windows.Forms.Padding(2);
            this.BackColor       = System.Drawing.Color.White;
            this.TopMost         = true;
            this.ShowInTaskbar   = false;
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Font            = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.Name            = "frmMenuScaleCal";
            this.Text            = "Scale Calibration";
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlTitle);
            this.Load += new System.EventHandler(this.frmMenuScaleCal_Load);

            this.ResumeLayout(false);
        }

        // One row: label, boxed read-only value, optional note to the right.
        private static void SetRow(System.Windows.Forms.Label label, string text,
                                   System.Windows.Forms.Label value, System.Windows.Forms.Label info,
                                   int y, System.Drawing.Font lf, System.Drawing.Font vf, System.Drawing.Font sf)
        {
            label.Text      = text; label.Font = lf; label.AutoSize = false;
            label.Location  = new System.Drawing.Point(8, y); label.Size = new System.Drawing.Size(220, 32);
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            value.Font        = vf; value.AutoSize = false;
            value.Location    = new System.Drawing.Point(236, y); value.Size = new System.Drawing.Size(160, 32);
            value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            value.TextAlign   = System.Drawing.ContentAlignment.MiddleCenter;

            if (info != null)
            {
                info.Font      = sf; info.AutoSize = false;
                info.Location  = new System.Drawing.Point(404, y); info.Size = new System.Drawing.Size(152, 32);
                info.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }
        }

        private System.Windows.Forms.Panel  pnlTitle;
        private System.Windows.Forms.Label  lblTitle;
        private System.Windows.Forms.Panel  pnlContent;
        private System.Windows.Forms.Label  lblRaw;
        private System.Windows.Forms.Label  lblRawVal;
        private System.Windows.Forms.Label  lblWeight;
        private System.Windows.Forms.Label  lblWeightVal;
        private System.Windows.Forms.Label  lblStable;
        private System.Windows.Forms.Label  lblStableVal;
        private System.Windows.Forms.Label  lblZero;
        private System.Windows.Forms.Label  lblZeroVal;
        private System.Windows.Forms.Label  lblZeroInfo;
        private System.Windows.Forms.Label  lblSpan;
        private System.Windows.Forms.Label  lblSpanVal;
        private System.Windows.Forms.Label  lblSpanInfo;
        private System.Windows.Forms.Button btnZero;
        private System.Windows.Forms.Button btnWeight;
        private System.Windows.Forms.Label  lblStatus;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;
    }
}
