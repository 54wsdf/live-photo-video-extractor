# Live 图转视频

<p align="center">
  <img src="src/LivePhotoVideoExtractor.App/Assets/app-icon-source.png" width="128" height="128" alt="Live 图转视频图标">
</p>

一个面向 Windows 10/11 的独立小工具，用于从安卓 Motion Photo 动态照片中无损提取原始 MP4。

## 下载

打开仓库右侧的 **Releases**，下载 `LivePhotoVideoExtractor.exe`。该文件为 Windows 10/11 x64 自包含单文件程序，不需要安装 .NET；启动后的应用名称仍为“Live 图转视频”。

> 当前公开版本没有 Authenticode 数字签名。Windows SmartScreen 可能显示“未知发布者”；可在 Release 页面核对 SHA-256 后再运行。

## 使用方法

1. 双击 `Live图转视频.exe`。
2. 将一个或多个 `.jpg` / `.jpeg` 动态照片拖入窗口；也可以点击“选择照片…”。
3. 视频会导出到每张照片所在的目录。
4. 在结果列表中选中成功项目，可点击“打开位置”。

## 输出规则

- 默认输出名为 `照片原文件名.mp4`。
- 如果同名文件已经存在，工具不会覆盖，依次使用 `照片原文件名_live_1.mp4`、`_live_2.mp4` 等名称。
- 导出过程不转码，保留动态照片中的原始视频画质和音频。
- 输出视频的文件创建时间和修改时间与源照片的修改时间一致。
- 原 JPG/JPEG 始终以只读方式打开，不会被修改、删除或剥离视频数据。

## 支持范围

- 支持常见 Google、三星、小米等安卓设备生成的 JPG/JPEG Motion Photo。
- 普通静态 JPG 会显示为“静态照片”，不会生成文件。
- iPhone Live Photo 通常是 HEIC/JPG 与 MOV 双文件，本工具不处理这种双文件组合，因为其中已经存在独立视频。

## 从源码构建

需要 .NET 8 SDK。运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build-release.ps1
```

脚本会先还原依赖并运行所有测试，再生成：

```text
artifacts\publish\Live图转视频.exe
```

最终 EXE 自包含，不要求目标电脑安装 .NET、Python 或 ExifTool。

应用图标的透明 PNG 母版和 Windows 多尺寸 ICO 均保存在源码中；可运行 `build-icon.ps1` 重新生成 ICO。

## 开源许可

本项目采用 [MIT License](LICENSE)。
