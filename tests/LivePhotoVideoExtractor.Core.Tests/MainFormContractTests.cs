using System.Runtime.ExceptionServices;
using LivePhotoVideoExtractor.App;

namespace LivePhotoVideoExtractor.Core.Tests;

public sealed class MainFormContractTests
{
    [Fact]
    public void MainForm_exposes_the_required_drag_and_drop_controls()
    {
        RunInSta(() =>
        {
            using var form = new MainForm();

            Assert.Equal("Live 图转视频", form.Text);
            Assert.True(form.AllowDrop);
            AssertControl<Panel>(form, "照片拖放区域", control => Assert.True(control.AllowDrop));
            AssertControl<Button>(form, "选择照片");
            AssertControl<ListView>(form, "处理结果");
            AssertControl<Label>(form, "处理汇总");
            AssertControl<Button>(form, "清空记录");
            AssertControl<Button>(form, "打开视频位置");
        });
    }

    private static void AssertControl<TControl>(
        Control root,
        string accessibleName,
        Action<TControl>? assertion = null)
        where TControl : Control
    {
        var control = Descendants(root)
            .OfType<TControl>()
            .SingleOrDefault(candidate => candidate.AccessibleName == accessibleName);
        Assert.NotNull(control);
        assertion?.Invoke(control);
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception caught)
            {
                exception = caught;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
