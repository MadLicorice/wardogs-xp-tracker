# Asset provenance

The executable embeds the OCR assets below. It checks each listed SHA-256 digest before it extracts or loads an OCR file. The separate branding section records the user-supplied logo.

## English language model

- Project: tesseract-ocr/tessdata_fast.
- License: Apache-2.0. See `licenses/Apache-2.0.txt`.
- Source commit: `87416418657359cb625c412a48b6e1d6d41c29bd`.
- Pinned source: https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/87416418657359cb625c412a48b6e1d6d41c29bd/eng.traineddata
- SHA-256: `7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2`.

## OCR engine and image library

- Package: Tesseract 5.2.0, by Charles Weld.
- Source package: https://api.nuget.org/v3-flatcontainer/tesseract/5.2.0/tesseract.5.2.0.nupkg
- Wrapper license: Apache-2.0. The wrapper also includes InteropDotNet under the MIT license.
- Native engine: `tesseract50.dll`. Its C API reports version `5.0.0` in this published package.
- Image library: `leptonica-1.82.0.dll`, BSD-2-Clause. See `licenses/Leptonica-BSD-2-Clause.txt`.
- Upstream build instructions: https://github.com/charlesw/tesseract/blob/master/docs/Compling_tesseract_and_leptonica.md
- The native library includes image codecs. Retain their upstream notices when distributing the complete package.

## Microsoft Visual C++ runtime

- Official package: https://aka.ms/vs/17/release/vc_redist.x64.exe
- Downloaded file version: `14.44.35211.0`.
- Downloaded package SHA-256: `cc0ff0eb1dc3f5188ae6300faef32bf5beeba4bdd6e8e445a9184072096b713b`.
- Package signature: Valid, Microsoft Corporation.
- Extracted DLL signatures: Valid, Microsoft Windows Software Compatibility Publisher.
- Files: `msvcp140.dll`, `vcruntime140.dll`, and `vcruntime140_1.dll`.
- License: See `licenses/Microsoft-VC-Runtime-license.rtf`.
- These files came from the signed redistributable package. They did not come from the Windows system directory.
- Extraction used the official WiX 3.11.2 `dark.exe`, then Windows `expand.exe` on `packages/vcRuntimeMinimum_amd64/cab1.cab`.
- WiX download: https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311-binaries.zip
- No runtime installer ran. App startup loads the extracted DLLs by absolute path before it starts Tesseract.
- Redistribution documentation: https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files?view=msvc-170

The C++ runtime files depend on each other and on the Windows 10/11 Universal C Runtime. No other Visual C++ DLL appeared in their normal PE imports.

## Native SHA-256 digests

| File | SHA-256 |
| --- | --- |
| leptonica-1.82.0.dll | dfcb3e6ed0b16bc55bfdbcf53543cfe42a354b87c3e35bd3a95eebf005d73e76 |
| tesseract50.dll | de4d04ec75095374d98f5dd7a60d14d7e2e0f76589db693eccf7ae658be8cb2b |
| msvcp140.dll | 0f885b509a685d2bbfa652fed26b5fb31d88fbdab0a978c641d1c7b8aa460aa9 |
| vcruntime140.dll | d5e4d9a3e835fa679450145d6a7d94e36573a509317111904d9b3712c30d9066 |
| vcruntime140_1.dll | 1f2d41c4aa5db0bc33ebf7b66d72943a817d7ce6cbe880502a9403823633093f |

## WARDOGS branding

- Asset: `branding/wardogs-logo.jpg`, embedded as `Assets/wardogs-logo.jpg` in the WPF app.
- Source: the user-supplied WARDOGS logo.
- Image: 128 × 128 black-and-white WARDOGS logo.
- SHA-256: `31b76b41b25d787a433f3cec06895923639e29537d964079a1998571a285d5c3`.
- Optional JPEG application metadata is removed. The compressed pixel data is unchanged.
- This is a WARDOGS game brand asset. The app's MIT license and the OCR dependencies' Apache licenses do not grant rights to it.
- No separate license for the supplied logo was provided. This record does not assign one.


## Orc XP application icon

- Assets: branding/wardogs-xp.png and branding/wardogs-xp.ico.
- Source: an orc avatar supplied by the user, edited with the built-in image_gen tool to add XP lettering.
- The PNG is 512 pixels square. The ICO has ten sizes from 16 through 256 pixels.
- Optional image metadata is removed from the PNG and every ICO frame.
- See branding/README.md for the prompt and conversion notes. The original avatar file and its path are not included.
