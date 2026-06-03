Place Tesseract language files in this folder for auto-tag OCR.

Required for the current configuration:
- eng.traineddata

Expected publish behavior:
- Files under tessdata are copied to build output and publish_output
- publish.bat warns if eng.traineddata is missing
- The app falls back to template matching when OCR data is unavailable
