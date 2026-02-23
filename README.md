# 📸 Batch Image Date

**Batch Image Date** is a fast, standalone desktop application designed to automate the process of batch watermarking photos with dates and exporting them into various formats, including multipage PDFs.

I originally built this application to help my wife optimize her daily workflow—replacing the manual, repetitive task of adding dates to hundreds of photos using Paint/Photoshop with a single-click automated solution.

<img width="1274" height="760" alt="Screenshot 2026-02-24 024317" src="https://github.com/user-attachments/assets/53b6397c-4d37-4294-ade3-6a414f4e564d" />

## ✨ Key Features

* **Smart EXIF Metadata Extraction:** Automatically reads the original creation date from the photo's EXIF data.
* **Fallback Logic:** If EXIF is missing, it automatically falls back to the file creation date or allows for a custom manual date.
* **Live Interactive Preview:** See exactly how your watermark looks before processing, with real-time zoom and pan features.
* **Customizable Watermarks:** Full control over font, size, position, and text color for perfect visibility.
* **Image Optimization:** Includes auto-orientation (fixes upside-down phone photos), resizing, and JPEG compression.
* **Advanced PDF Export:** * Export as watermarked images.
    * Export each photo as an individual PDF.
    * Combine hundreds of photos into a **Single Multipage PDF**.
* **Portable & Lightweight:** Built as a self-contained `.exe`. No installation required—just download and run.

## 🛠️ Tech Stack

This application was built using **C# / .NET / WPF** and leverages the following libraries:
* [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) - For high-performance image processing.
* [QuestPDF](https://github.com/QuestPDF/QuestPDF) - For generating optimized PDF documents.
* [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet) - For extracting camera metadata.
* [WPF-UI](https://github.com/lepoco/wpfui) - For a modern Windows 11 Fluent interface.

## 🚀 How to Use

1. Go to the **Releases** tab on this repository.
2. Download the latest `BatchImageDate.exe`.
3. Select your input folder and configure your watermark settings.
4. Choose your output format and click **Start Batch Process**.

## 🤝 Contributing
Feel free to open issues or submit pull requests if you have ideas for improvements!

## 📄 License
This project is licensed under the MIT License.
