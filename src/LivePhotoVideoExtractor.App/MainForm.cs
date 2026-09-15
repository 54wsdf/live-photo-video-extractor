using System.Diagnostics;
using LivePhotoVideoExtractor.Core;

namespace LivePhotoVideoExtractor.App;

public partial class MainForm : Form
{
    private static readonly Color DropZoneNormalColor = Color.FromArgb(247, 249, 252);
    private static readonly Color DropZoneActiveColor = Color.FromArgb(226, 239, 255);
    private readonly BatchExtractor _batchExtractor = new(
        new MotionPhotoExtractor(new WindowsVideoOrientationNormalizer()));
    private bool _isProcessing;

    public MainForm()
    {
        InitializeComponent();
        RegisterDropTarget(this);
        RegisterDropTarget(dropPanel);
    }

    private void RegisterDropTarget(Control control)
    {
        control.DragEnter += HandleDragEnter;
        control.DragLeave += HandleDragLeave;
        control.DragDrop += HandleDragDrop;
    }

    private void HandleDragEnter(object? sender, DragEventArgs eventArgs)
    {
        var paths = GetDroppedPaths(eventArgs);
        var hasSupportedPhoto = paths.Any(IsSupportedPhotoPath);
        eventArgs.Effect = !_isProcessing && hasSupportedPhoto
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        dropPanel.BackColor = eventArgs.Effect == DragDropEffects.Copy
            ? DropZoneActiveColor
            : DropZoneNormalColor;
    }

    private void HandleDragLeave(object? sender, EventArgs eventArgs)
    {
        dropPanel.BackColor = DropZoneNormalColor;
    }

    private async void HandleDragDrop(object? sender, DragEventArgs eventArgs)
    {
        dropPanel.BackColor = DropZoneNormalColor;
        if (_isProcessing)
        {
            return;
        }

        var paths = GetDroppedPaths(eventArgs);
        await ProcessPathsAsync(paths);
    }

    private async void SelectPhotosButton_Click(object? sender, EventArgs eventArgs)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择安卓动态照片",
            Filter = "JPG 动态照片 (*.jpg;*.jpeg)|*.jpg;*.jpeg|所有文件 (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true,
            RestoreDirectory = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await ProcessPathsAsync(dialog.FileNames);
        }
    }

    private async Task ProcessPathsAsync(IEnumerable<string> paths)
    {
        var inputs = paths.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        if (_isProcessing || inputs.Length == 0)
        {
            return;
        }

        SetProcessingState(true);
        progressBar.Maximum = inputs.Length;
        progressBar.Value = 0;
        summaryLabel.Text = $"正在处理 0/{inputs.Length}…";

        var progress = new Progress<BatchProgress>(UpdateProgress);
        try
        {
            var results = await _batchExtractor.ExtractAsync(inputs, progress);
            var success = results.Count(result => result.Status == ExtractionStatus.Success);
            var skipped = results.Count(result => result.Status is ExtractionStatus.NoEmbeddedVideo or ExtractionStatus.UnsupportedInput);
            var failed = results.Count - success - skipped;
            summaryLabel.Text = $"完成：成功 {success}，跳过 {skipped}，失败 {failed}";
        }
        catch (Exception exception)
        {
            summaryLabel.Text = "处理意外中断";
            MessageBox.Show(
                this,
                exception.Message,
                "Live 图转视频",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetProcessingState(false);
        }
    }

    private void UpdateProgress(BatchProgress progress)
    {
        progressBar.Maximum = Math.Max(1, progress.Total);
        progressBar.Value = Math.Min(progress.Completed, progressBar.Maximum);
        resultsListView.Items.Add(UiResultItem.Create(progress.Current));
        resultsListView.Items[^1].EnsureVisible();
        summaryLabel.Text = $"正在处理 {progress.Completed}/{progress.Total}…";
    }

    private void SetProcessingState(bool isProcessing)
    {
        _isProcessing = isProcessing;
        selectPhotosButton.Enabled = !isProcessing;
        clearButton.Enabled = !isProcessing && resultsListView.Items.Count > 0;
        UseWaitCursor = isProcessing;
    }

    private void ClearButton_Click(object? sender, EventArgs eventArgs)
    {
        resultsListView.Items.Clear();
        progressBar.Value = 0;
        summaryLabel.Text = "等待照片";
        clearButton.Enabled = false;
        openLocationButton.Enabled = false;
    }

    private void ResultsListView_SelectedIndexChanged(object? sender, EventArgs eventArgs)
    {
        openLocationButton.Enabled = SelectedOutputPath() is not null;
    }

    private void OpenLocationButton_Click(object? sender, EventArgs eventArgs)
    {
        var outputPath = SelectedOutputPath();
        if (outputPath is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{outputPath}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                this,
                $"无法打开文件位置：{exception.Message}",
                "Live 图转视频",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private string? SelectedOutputPath()
    {
        return resultsListView.SelectedItems.Count == 1
            ? resultsListView.SelectedItems[0].Tag as string
            : null;
    }

    private static string[] GetDroppedPaths(DragEventArgs eventArgs)
    {
        return eventArgs.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? eventArgs.Data.GetData(DataFormats.FileDrop) as string[] ?? []
            : [];
    }

    private static bool IsSupportedPhotoPath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }
}
