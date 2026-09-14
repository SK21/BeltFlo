using BeltFlo.Language;

namespace BeltFlo.Forms
{
    partial class frmMenuCrops
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
            this.lbCrops       = new System.Windows.Forms.ListBox();
            this.pnlEdit       = new System.Windows.Forms.Panel();
            this.lblCropName   = new System.Windows.Forms.Label();
            this.txtCropName   = new System.Windows.Forms.TextBox();
            this.btnNew        = new System.Windows.Forms.Button();
            this.btnSave       = new System.Windows.Forms.Button();
            this.btnDelete     = new System.Windows.Forms.Button();
            this.btnCropsClose = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // ── Title bar ────────────────────────────────────────────────────
            this.pnlTitle.Dock   = System.Windows.Forms.DockStyle.Top;
            this.pnlTitle.Height = 48;

            this.lblTitle.Text      = Lang.lgTitleCrops;
            this.lblTitle.Font      = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(180, 200, 220);
            this.lblTitle.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.AutoSize  = false;
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.pnlTitle.Controls.Add(this.lblTitle);

            // ── Content ───────────────────────────────────────────────────────
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;

            this.lbCrops.Location      = new System.Drawing.Point(4, 4);
            this.lbCrops.Size          = new System.Drawing.Size(552, 170);
            this.lbCrops.Font          = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.lbCrops.SelectedIndexChanged += new System.EventHandler(this.lbCrops_SelectedIndexChanged);

            // Edit panel
            this.pnlEdit.Location  = new System.Drawing.Point(4, 180);
            this.pnlEdit.Size      = new System.Drawing.Size(552, 44);

            var lf = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);
            var vf = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);

            // Name
            this.lblCropName.Text = Lang.lgName; this.lblCropName.Font = lf; this.lblCropName.Location = new System.Drawing.Point(8, 4); this.lblCropName.AutoSize = false; this.lblCropName.Size = new System.Drawing.Size(220, 32);
            this.txtCropName.Font = vf; this.txtCropName.Location = new System.Drawing.Point(236, 4); this.txtCropName.Height = 32; this.txtCropName.Width = 310;
            pnlEdit.Controls.Add(this.lblCropName); pnlEdit.Controls.Add(this.txtCropName);

            this.pnlContent.Controls.Add(this.lbCrops);
            this.pnlContent.Controls.Add(this.pnlEdit);

            // Buttons row
            this.btnNew.Text = Lang.lgNew; this.btnNew.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold); this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat; this.btnNew.Location = new System.Drawing.Point(8, 232); this.btnNew.Size = new System.Drawing.Size(130, 44);
            this.btnSave.Text = Lang.lgSave; this.btnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold); this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat; this.btnSave.Location = new System.Drawing.Point(146, 232); this.btnSave.Size = new System.Drawing.Size(130, 44);
            this.btnDelete.Text = Lang.lgDelete; this.btnDelete.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold); this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat; this.btnDelete.Location = new System.Drawing.Point(284, 232); this.btnDelete.Size = new System.Drawing.Size(130, 44);
            this.btnCropsClose.Text = Lang.lgClose; this.btnCropsClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold); this.btnCropsClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat; this.btnCropsClose.Location = new System.Drawing.Point(422, 232); this.btnCropsClose.Size = new System.Drawing.Size(130, 44);
            this.pnlContent.Controls.AddRange(new System.Windows.Forms.Control[] { btnNew, btnSave, btnDelete, btnCropsClose });

            btnNew.Click        += new System.EventHandler(this.btnNew_Click);
            btnSave.Click       += new System.EventHandler(this.btnSave_Click);
            btnDelete.Click     += new System.EventHandler(this.btnDelete_Click);
            btnCropsClose.Click += new System.EventHandler(this.btnCropsClose_Click);

            // ── Form ──────────────────────────────────────────────────────────
            this.ClientSize      = new System.Drawing.Size(564, 336);
            this.MinimumSize     = new System.Drawing.Size(564, 336);
            this.MaximumSize     = new System.Drawing.Size(564, 336);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Padding         = new System.Windows.Forms.Padding(2);
            this.BackColor       = System.Drawing.Color.White;
            this.TopMost         = true;
            this.ShowInTaskbar   = false;
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Font            = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.Name            = "frmMenuCrops";
            this.Text            = "Crops";
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlTitle);
            this.Load += new System.EventHandler(this.frmMenuCrops_Load);

            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel   pnlTitle;
        private System.Windows.Forms.Label   lblTitle;
        private System.Windows.Forms.Panel   pnlContent;
        private System.Windows.Forms.ListBox lbCrops;
        private System.Windows.Forms.Panel   pnlEdit;
        private System.Windows.Forms.Label   lblCropName;
        private System.Windows.Forms.TextBox txtCropName;
        private System.Windows.Forms.Button  btnNew;
        private System.Windows.Forms.Button  btnSave;
        private System.Windows.Forms.Button  btnDelete;
        private System.Windows.Forms.Button  btnCropsClose;
    }
}
