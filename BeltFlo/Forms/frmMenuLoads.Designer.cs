using BeltFlo.Language;

namespace BeltFlo.Forms
{
    partial class frmMenuLoads
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlTitle       = new System.Windows.Forms.Panel();
            this.lblTitle       = new System.Windows.Forms.Label();
            this.pnlContent     = new System.Windows.Forms.Panel();
            this.lvLoads        = new System.Windows.Forms.ListView();
            this.lblLoadInfo    = new System.Windows.Forms.Label();
            this.lblFlag        = new System.Windows.Forms.Label();
            this.btnFlagNone    = new System.Windows.Forms.Button();
            this.btnFlagWet     = new System.Windows.Forms.Button();
            this.btnFlagSpoiled     = new System.Windows.Forms.Button();
            this.btnFlagTrash   = new System.Windows.Forms.Button();
            this.btnFlagStones  = new System.Windows.Forms.Button();
            this.lblMonitor     = new System.Windows.Forms.Label();
            this.lblMonitorVal  = new System.Windows.Forms.Label();
            this.lblTicket      = new System.Windows.Forms.Label();
            this.lblTicketVal   = new System.Windows.Forms.Label();
            this.lblDiff        = new System.Windows.Forms.Label();
            this.lblDiffVal     = new System.Windows.Forms.Label();
            this.lblFactor      = new System.Windows.Forms.Label();
            this.lblFactorVal   = new System.Windows.Forms.Label();
            this.btnWeightOnly  = new System.Windows.Forms.Button();
            this.btnCorrectLoad = new System.Windows.Forms.Button();
            this.btnCorrectJob  = new System.Windows.Forms.Button();
            this.btnUpdateCal   = new System.Windows.Forms.Button();
            this.lblStatus      = new System.Windows.Forms.Label();
            this.btnRename      = new System.Windows.Forms.Button();
            this.btnReopen      = new System.Windows.Forms.Button();
            this.btnDelete      = new System.Windows.Forms.Button();
            this.btnClose       = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // ── Title bar ────────────────────────────────────────────────────
            this.pnlTitle.Dock   = System.Windows.Forms.DockStyle.Top;
            this.pnlTitle.Height = 48;

            this.lblTitle.Text      = Lang.lgTitleLoads;
            this.lblTitle.Font      = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(180, 200, 220);
            this.lblTitle.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.AutoSize  = false;
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.pnlTitle.Controls.Add(this.lblTitle);

            // ── Content ───────────────────────────────────────────────────────
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;

            var labelFont  = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);
            var inputFont  = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            var buttonFont = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);

            // ── List ──────────────────────────────────────────────────────────
            this.lvLoads.Location      = new System.Drawing.Point(4, 4);
            this.lvLoads.Size          = new System.Drawing.Size(690, 206);
            this.lvLoads.View          = System.Windows.Forms.View.Details;
            this.lvLoads.FullRowSelect = true;
            this.lvLoads.HideSelection = false;
            this.lvLoads.MultiSelect   = false;
            this.lvLoads.HeaderStyle   = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvLoads.BorderStyle   = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lvLoads.Font          = inputFont;
            this.lvLoads.Columns.Add(Lang.lgLoad,       60);
            this.lvLoads.Columns.Add(Lang.lgColJob,    140);
            this.lvLoads.Columns.Add(Lang.lgTruck,     100);
            this.lvLoads.Columns.Add(Lang.lgColMonitor, 85);
            this.lvLoads.Columns.Add(Lang.lgColTicket,  85);
            this.lvLoads.Columns.Add(Lang.lgColDiff,    75);
            this.lvLoads.Columns.Add(Lang.lgColStatus, 120);
            this.lvLoads.OwnerDraw = true;
            this.lvLoads.DrawColumnHeader     += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.lvLoads_DrawColumnHeader);
            this.lvLoads.DrawSubItem          += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.lvLoads_DrawSubItem);
            this.lvLoads.SelectedIndexChanged += new System.EventHandler(this.lvLoads_SelectedIndexChanged);

            // Load, job and times of the selected load
            this.lblLoadInfo.Font      = inputFont;
            this.lblLoadInfo.Location  = new System.Drawing.Point(8, 216);
            this.lblLoadInfo.Size      = new System.Drawing.Size(682, 28);
            this.lblLoadInfo.AutoSize  = false;
            this.lblLoadInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ── Flag: one tap each, the lit one is the load's flag ────────────
            this.lblFlag.Text     = Lang.lgFlag; this.lblFlag.Font = labelFont;
            this.lblFlag.Location = new System.Drawing.Point(8, 256); this.lblFlag.Size = new System.Drawing.Size(108, 26); this.lblFlag.AutoSize = false;

            var flagButtons = new[] { btnFlagNone, btnFlagWet, btnFlagSpoiled, btnFlagTrash, btnFlagStones };
            var flagTexts   = new[] { Lang.lgFlagNone, Lang.lgFlagWet, Lang.lgFlagSpoiled, Lang.lgFlagTrash, Lang.lgFlagStones };
            for (int i = 0; i < flagButtons.Length; i++)
            {
                flagButtons[i].Text      = flagTexts[i];
                flagButtons[i].Font      = buttonFont;
                flagButtons[i].FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                flagButtons[i].Location  = new System.Drawing.Point(120 + i * 114, 250);
                flagButtons[i].Size      = new System.Drawing.Size(108, 40);
            }

            // ── Weights: tap the ticket box for the numpad ────────────────────
            SetLabel(this.lblMonitor, Lang.lgMonitorWeight, labelFont, 8,   306);
            SetBox  (this.lblMonitorVal, inputFont, 160, 300);
            SetLabel(this.lblTicket,  Lang.lgTicketWeight,  labelFont, 360, 306);
            SetBox  (this.lblTicketVal,  inputFont, 504, 300);
            this.lblTicketVal.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblTicketVal.Click += new System.EventHandler(this.lblTicketVal_Click);

            SetLabel(this.lblDiff,    Lang.lgDifference,    labelFont, 8,   350);
            SetBox  (this.lblDiffVal,    inputFont, 160, 344);
            SetLabel(this.lblFactor,  Lang.lgFactor,        labelFont, 360, 350);
            SetBox  (this.lblFactorVal,  inputFont, 504, 344);

            // ── What to do with the ticket ────────────────────────────────────
            // Correct Job takes Correct This Load's place when the scale weighs into a tank.
            SetButton(this.btnWeightOnly,  Lang.lgSaveWeightOnly,    buttonFont, 4,   394, 224);
            SetButton(this.btnCorrectLoad, Lang.lgCorrectLoad,       buttonFont, 233, 394, 224);
            SetButton(this.btnCorrectJob,  Lang.lgCorrectJob,        buttonFont, 233, 394, 224);
            SetButton(this.btnUpdateCal,   Lang.lgUpdateCalibration, buttonFont, 462, 394, 228);
            this.btnCorrectJob.Visible = false;
            this.btnWeightOnly.Click  += new System.EventHandler(this.btnWeightOnly_Click);
            this.btnCorrectLoad.Click += new System.EventHandler(this.btnCorrectLoad_Click);
            this.btnCorrectJob.Click  += new System.EventHandler(this.btnCorrectJob_Click);
            this.btnUpdateCal.Click   += new System.EventHandler(this.btnUpdateCal_Click);

            this.lblStatus.Font      = new System.Drawing.Font("Microsoft Sans Serif", 11.25F);
            this.lblStatus.Location  = new System.Drawing.Point(8, 448);
            this.lblStatus.Size      = new System.Drawing.Size(682, 44);
            this.lblStatus.AutoSize  = false;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ── Bottom row ────────────────────────────────────────────────────
            SetButton(this.btnRename, Lang.lgRename, buttonFont, 4,   498, 130);
            SetButton(this.btnReopen, Lang.lgReopen, buttonFont, 139, 498, 130);
            SetButton(this.btnDelete, Lang.lgDelete, buttonFont, 274, 498, 130);
            SetButton(this.btnClose,  Lang.lgClose,  buttonFont, 560, 498, 130);
            this.btnRename.Click += new System.EventHandler(this.btnRename_Click);
            this.btnReopen.Click += new System.EventHandler(this.btnReopen_Click);
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            this.btnClose.Click  += new System.EventHandler(this.btnClose_Click);

            this.pnlContent.Controls.AddRange(new System.Windows.Forms.Control[] {
                lvLoads, lblLoadInfo,
                lblFlag, btnFlagNone, btnFlagWet, btnFlagSpoiled, btnFlagTrash, btnFlagStones,
                lblMonitor, lblMonitorVal, lblTicket, lblTicketVal,
                lblDiff, lblDiffVal, lblFactor, lblFactorVal,
                btnWeightOnly, btnCorrectLoad, btnCorrectJob, btnUpdateCal,
                lblStatus,
                btnRename, btnReopen, btnDelete, btnClose });

            // ── Form ──────────────────────────────────────────────────────────
            this.ClientSize      = new System.Drawing.Size(702, 602);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Padding         = new System.Windows.Forms.Padding(2);
            this.BackColor       = System.Drawing.Color.White;
            this.TopMost         = true;
            this.ShowInTaskbar   = false;
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Font            = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.Name            = "frmMenuLoads";
            this.Text            = "Loads";
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlTitle);
            this.Load += new System.EventHandler(this.frmMenuLoads_Load);

            this.ResumeLayout(false);
        }

        private static void SetLabel(System.Windows.Forms.Label l, string text, System.Drawing.Font font, int x, int y)
        {
            l.Text     = text;
            l.Font     = font;
            l.Location = new System.Drawing.Point(x, y);
            l.Size     = new System.Drawing.Size(140, 26);   // clears the box beside it (right column: 360 + 140 < 504)
            l.AutoSize = false;
        }

        private static void SetBox(System.Windows.Forms.Label l, System.Drawing.Font font, int x, int y)
        {
            l.Font        = font;
            l.Location    = new System.Drawing.Point(x, y);
            l.Size        = new System.Drawing.Size(186, 36);
            l.AutoSize    = false;
            l.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            l.TextAlign   = System.Drawing.ContentAlignment.MiddleCenter;
            l.Text        = "--";
        }

        private static void SetButton(System.Windows.Forms.Button b, string text, System.Drawing.Font font, int x, int y, int width)
        {
            b.Text      = text;
            b.Font      = font;
            b.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            b.Location  = new System.Drawing.Point(x, y);
            b.Size      = new System.Drawing.Size(width, 48);
        }

        private System.Windows.Forms.Panel    pnlTitle;
        private System.Windows.Forms.Label    lblTitle;
        private System.Windows.Forms.Panel    pnlContent;
        private System.Windows.Forms.ListView lvLoads;
        private System.Windows.Forms.Label    lblLoadInfo;
        private System.Windows.Forms.Label    lblFlag;
        private System.Windows.Forms.Button   btnFlagNone;
        private System.Windows.Forms.Button   btnFlagWet;
        private System.Windows.Forms.Button   btnFlagSpoiled;
        private System.Windows.Forms.Button   btnFlagTrash;
        private System.Windows.Forms.Button   btnFlagStones;
        private System.Windows.Forms.Label    lblMonitor;
        private System.Windows.Forms.Label    lblMonitorVal;
        private System.Windows.Forms.Label    lblTicket;
        private System.Windows.Forms.Label    lblTicketVal;
        private System.Windows.Forms.Label    lblDiff;
        private System.Windows.Forms.Label    lblDiffVal;
        private System.Windows.Forms.Label    lblFactor;
        private System.Windows.Forms.Label    lblFactorVal;
        private System.Windows.Forms.Button   btnWeightOnly;
        private System.Windows.Forms.Button   btnCorrectLoad;
        private System.Windows.Forms.Button   btnCorrectJob;
        private System.Windows.Forms.Button   btnUpdateCal;
        private System.Windows.Forms.Label    lblStatus;
        private System.Windows.Forms.Button   btnRename;
        private System.Windows.Forms.Button   btnReopen;
        private System.Windows.Forms.Button   btnDelete;
        private System.Windows.Forms.Button   btnClose;
    }
}
