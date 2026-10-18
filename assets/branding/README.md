# App artwork

The WARDOGS header logo and orc source artwork were supplied for this app. The orc application icon is a derivative made with the built-in image_gen tool.

Prompt: Preserve the green pixel-art orc face, red eyes, tusks, dark hood, and red accents. Superimpose the exact text "XP" in large cream/gold letters with a dark outline across the face. Keep the eyes visible and the letters readable at taskbar sizes. Fill a square without extra text, a frame, or a mockup.

wardogs-xp.png is the 512-pixel source asset. wardogs-xp.ico contains 16, 20, 24, 32, 40, 48, 64, 96, 128, and 256-pixel PNG frames. Optional image metadata is absent from these assets. Format structures and pixel data remain.

The optional tools/BuildApplicationIcon.ps1 converter uses PowerShell 7.6 on Windows and its drawing assemblies. Pass a square input image and an output directory. The app build uses the committed icon and does not require this conversion step. tools/StripAssetMetadata.py uses Python's standard library for lossless container cleanup.
