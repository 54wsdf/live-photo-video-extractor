using LivePhotoVideoExtractor.Core;

namespace LivePhotoVideoExtractor.App;

internal static class UiResultItem
{
    public static ListViewItem Create(ExtractionResult result)
    {
        var item = new ListViewItem(Path.GetFileName(result.SourcePath));
        item.SubItems.Add(StatusText(result.Status));
        item.SubItems.Add(result.OutputPath ?? result.Message);
        item.Tag = result.OutputPath;
        item.ForeColor = StatusColor(result.Status);
        item.ToolTipText = result.Message;
        return item;
    }

    private static string StatusText(ExtractionStatus status)
    {
        return status switch
        {
            ExtractionStatus.Success => "成功",
            ExtractionStatus.NoEmbeddedVideo => "静态照片",
            ExtractionStatus.UnsupportedInput => "不支持",
            ExtractionStatus.Failed => "失败",
            _ => "未知",
        };
    }

    private static Color StatusColor(ExtractionStatus status)
    {
        return status switch
        {
            ExtractionStatus.Success => Color.FromArgb(22, 101, 52),
            ExtractionStatus.NoEmbeddedVideo or ExtractionStatus.UnsupportedInput => Color.FromArgb(146, 64, 14),
            ExtractionStatus.Failed => Color.FromArgb(185, 28, 28),
            _ => SystemColors.WindowText,
        };
    }
}
