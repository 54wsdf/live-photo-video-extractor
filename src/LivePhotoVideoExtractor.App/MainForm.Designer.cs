#nullable enable

namespace LivePhotoVideoExtractor.App;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel rootLayout = null!;
    private Panel headerPanel = null!;
    private Label titleLabel = null!;
    private Label subtitleLabel = null!;
    private Panel dropPanel = null!;
    private TableLayoutPanel dropLayout = null!;
    private Label dropTitleLabel = null!;
    private Label dropHintLabel = null!;
    private Button selectPhotosButton = null!;
    private GroupBox resultsGroupBox = null!;
    private ListView resultsListView = null!;
    private ColumnHeader fileColumn = null!;
    private ColumnHeader statusColumn = null!;
    private ColumnHeader outputColumn = null!;
    private TableLayoutPanel footerLayout = null!;
    private Label summaryLabel = null!;
    private FlowLayoutPanel actionsPanel = null!;
    private Button openLocationButton = null!;
    private Button clearButton = null!;
    private ProgressBar progressBar = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        titleLabel = new Label();
        subtitleLabel = new Label();
        dropPanel = new Panel();
        dropLayout = new TableLayoutPanel();
        dropTitleLabel = new Label();
        dropHintLabel = new Label();
        selectPhotosButton = new Button();
        resultsGroupBox = new GroupBox();
        resultsListView = new ListView();
        fileColumn = new ColumnHeader();
        statusColumn = new ColumnHeader();
        outputColumn = new ColumnHeader();
        footerLayout = new TableLayoutPanel();
        summaryLabel = new Label();
        actionsPanel = new FlowLayoutPanel();
        openLocationButton = new Button();
        clearButton = new Button();
        progressBar = new ProgressBar();
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        dropPanel.SuspendLayout();
        dropLayout.SuspendLayout();
        resultsGroupBox.SuspendLayout();
        footerLayout.SuspendLayout();
        actionsPanel.SuspendLayout();
        SuspendLayout();

        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(dropPanel, 0, 1);
        rootLayout.Controls.Add(resultsGroupBox, 0, 2);
        rootLayout.Controls.Add(footerLayout, 0, 3);
        rootLayout.Controls.Add(progressBar, 0, 4);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Padding = new Padding(18, 14, 18, 16);
        rootLayout.RowCount = 5;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 154F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Dock = DockStyle.Fill;

        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold);
        titleLabel.ForeColor = Color.FromArgb(31, 41, 55);
        titleLabel.Location = new Point(0, 0);
        titleLabel.Text = "Live 图转视频";

        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Microsoft YaHei UI", 9F);
        subtitleLabel.ForeColor = Color.FromArgb(90, 101, 117);
        subtitleLabel.Location = new Point(2, 39);
        subtitleLabel.Text = "无损提取安卓动态照片中的原始 MP4，不修改照片";

        dropPanel.AccessibleName = "照片拖放区域";
        dropPanel.AllowDrop = true;
        dropPanel.BackColor = DropZoneNormalColor;
        dropPanel.BorderStyle = BorderStyle.FixedSingle;
        dropPanel.Controls.Add(dropLayout);
        dropPanel.Dock = DockStyle.Fill;
        dropPanel.Margin = new Padding(0, 4, 0, 10);

        dropLayout.ColumnCount = 1;
        dropLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        dropLayout.Controls.Add(dropTitleLabel, 0, 0);
        dropLayout.Controls.Add(dropHintLabel, 0, 1);
        dropLayout.Controls.Add(selectPhotosButton, 0, 2);
        dropLayout.Dock = DockStyle.Fill;
        dropLayout.Padding = new Padding(12, 18, 12, 14);
        dropLayout.RowCount = 3;
        dropLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        dropLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        dropLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

        dropTitleLabel.Dock = DockStyle.Fill;
        dropTitleLabel.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
        dropTitleLabel.ForeColor = Color.FromArgb(37, 99, 235);
        dropTitleLabel.Text = "把 JPG / JPEG 动态照片拖到这里";
        dropTitleLabel.TextAlign = ContentAlignment.MiddleCenter;

        dropHintLabel.Dock = DockStyle.Fill;
        dropHintLabel.ForeColor = Color.FromArgb(100, 116, 139);
        dropHintLabel.Text = "支持多选 · 视频导出到照片同目录 · 已有文件绝不覆盖";
        dropHintLabel.TextAlign = ContentAlignment.MiddleCenter;

        selectPhotosButton.AccessibleName = "选择照片";
        selectPhotosButton.Anchor = AnchorStyles.None;
        selectPhotosButton.AutoSize = true;
        selectPhotosButton.BackColor = Color.FromArgb(37, 99, 235);
        selectPhotosButton.FlatAppearance.BorderSize = 0;
        selectPhotosButton.FlatStyle = FlatStyle.Flat;
        selectPhotosButton.ForeColor = Color.White;
        selectPhotosButton.Padding = new Padding(18, 5, 18, 5);
        selectPhotosButton.Text = "选择照片…";
        selectPhotosButton.UseVisualStyleBackColor = false;
        selectPhotosButton.Click += SelectPhotosButton_Click;

        resultsGroupBox.Controls.Add(resultsListView);
        resultsGroupBox.Dock = DockStyle.Fill;
        resultsGroupBox.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        resultsGroupBox.Margin = new Padding(0);
        resultsGroupBox.Padding = new Padding(10, 8, 10, 10);
        resultsGroupBox.Text = "处理结果";

        resultsListView.AccessibleName = "处理结果";
        resultsListView.Columns.AddRange([fileColumn, statusColumn, outputColumn]);
        resultsListView.Dock = DockStyle.Fill;
        resultsListView.Font = new Font("Microsoft YaHei UI", 9F);
        resultsListView.FullRowSelect = true;
        resultsListView.GridLines = true;
        resultsListView.HideSelection = false;
        resultsListView.MultiSelect = false;
        resultsListView.View = View.Details;
        resultsListView.SelectedIndexChanged += ResultsListView_SelectedIndexChanged;

        fileColumn.Text = "照片";
        fileColumn.Width = 210;
        statusColumn.Text = "状态";
        statusColumn.Width = 90;
        outputColumn.Text = "详情 / 输出位置";
        outputColumn.Width = 390;

        footerLayout.ColumnCount = 2;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footerLayout.Controls.Add(summaryLabel, 0, 0);
        footerLayout.Controls.Add(actionsPanel, 1, 0);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.RowCount = 1;
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        summaryLabel.AccessibleName = "处理汇总";
        summaryLabel.Dock = DockStyle.Fill;
        summaryLabel.ForeColor = Color.FromArgb(71, 85, 105);
        summaryLabel.Text = "等待照片";
        summaryLabel.TextAlign = ContentAlignment.MiddleLeft;

        actionsPanel.AutoSize = true;
        actionsPanel.Controls.Add(openLocationButton);
        actionsPanel.Controls.Add(clearButton);
        actionsPanel.Dock = DockStyle.Fill;
        actionsPanel.FlowDirection = FlowDirection.LeftToRight;
        actionsPanel.Padding = new Padding(0, 7, 0, 0);
        actionsPanel.WrapContents = false;

        openLocationButton.AccessibleName = "打开视频位置";
        openLocationButton.AutoSize = true;
        openLocationButton.Enabled = false;
        openLocationButton.Text = "打开位置";
        openLocationButton.Click += OpenLocationButton_Click;

        clearButton.AccessibleName = "清空记录";
        clearButton.AutoSize = true;
        clearButton.Enabled = false;
        clearButton.Margin = new Padding(8, 0, 0, 0);
        clearButton.Text = "清空记录";
        clearButton.Click += ClearButton_Click;

        progressBar.AccessibleName = "处理进度";
        progressBar.Dock = DockStyle.Fill;
        progressBar.Margin = new Padding(0, 1, 0, 0);
        progressBar.Style = ProgressBarStyle.Continuous;

        AllowDrop = true;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.White;
        ClientSize = new Size(760, 560);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9F);
        MinimumSize = new Size(660, 470);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Live 图转视频";
        rootLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        dropPanel.ResumeLayout(false);
        dropLayout.ResumeLayout(false);
        dropLayout.PerformLayout();
        resultsGroupBox.ResumeLayout(false);
        footerLayout.ResumeLayout(false);
        footerLayout.PerformLayout();
        actionsPanel.ResumeLayout(false);
        actionsPanel.PerformLayout();
        ResumeLayout(false);
    }
}
