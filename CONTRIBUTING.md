# 参与贡献

欢迎提交问题报告与 Pull Request。

## 本地开发

1. 安装 .NET 8 SDK。
2. 运行 `dotnet test .\LivePhotoVideoExtractor.sln --configuration Release`。
3. 修改提取逻辑时，请先添加能复现预期行为的测试。
4. 运行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build-release.ps1` 验证独立 EXE 构建。

请勿提交真实照片、生成的视频、`artifacts`、`.dotnet`、`bin` 或 `obj` 目录。
