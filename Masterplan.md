# 🛠️ Masterplan: Image & PDF Batch Studio

## 1. Project Overview & Goal
Develop a lightning-fast, modern, and visually stunning C# .NET 10 desktop application for batch processing images. The primary focus is to watermark photos with dates (extracted from EXIF data or input manually), convert/optimize file formats and sizes, and export them seamlessly with advanced routing (Images or PDF documents).

## 2. Tech Stack
* **Language / Platform:** C# / .NET 10
* **UI Framework:** WPF (Windows Presentation Foundation)
* **UI Library:** Custom built native Dark Mode XAML templates (for optimal performance and tailored aesthetics).
* **Architecture Pattern:** MVVM (Model-View-ViewModel) approach for backend logic.
* **Image Processing Engine:** `SixLabors.ImageSharp` (Fast, lightweight, background-thread friendly). Additional plugin/library (e.g., `Magick.NET`) for HEIC format support.
* **PDF Generator:** `QuestPDF` (or `PdfSharp`).

## 3. Core Features
* **Smart Date Extraction:**
  * Automatically retrieves the original Date Taken from EXIF Metadata.
  * Fallback to manual custom text/date if EXIF is missing.
* **Full Design Control:**
  * Font family, custom Color Blocks for Text and Stroke (outline).
  * Proportional Sizing: Text scales automatically based on image resolution (1-10 Dynamic Slider).
  * 5 Anchor Positions: Top-Left, Top-Right, Bottom-Left, Bottom-Right, Center.
* **Advanced File Management:**
  * **Deep Search:** Ability to import images from a root folder including all subfolders.
  * **Dual List system:** Select and move specific files between "Excluded Files" and "Ready to Process" queues. Keyboard `Delete` support.
  * **Drag & Drop:** Directly drop files/folders into the application to populate the processing list.
* **Optimization & Conversion:**
  * Convert all imported formats (PNG, HEIC, etc.) to standard JPEG.
  * Target file size (MB) limitation.
  * Dynamic JPEG Quality compression slider.
* **Flexible Export Routing:**
  * Replace existing pictures (Auto-disabled for PDFs).
  * Save in original folder.
  * Save to a Custom Directory.
  * **Custom PDF Name:** Ability to set custom generated file name for Multipage PDF exports.
* **3 Export Modes:**
  1. Standard Images (JPEG saved with chosen routing).
  2. Single PDF: Each photo saved as an individual PDF file with its date.
  3. Multipage PDF: All selected photos combined into one large PDF.
* **Persistent Preferences:** Automatically saves and loads all user settings across sessions using JSON.
* **Live Preview:** Real-time visual feedback showing exactly how the text will look on a selected photo before starting the batch process, with dynamic bounding box constraints.

## 4. User Interface (UI / UX)
The application is exclusively **Dark Mode** and visually divided into 3 main columns:

1. **Settings & Import (Left):**
   * Date source inputs.
   * Folder browsing with Deep Search checkbox.
   * Font styling, proportional size, position dropdowns, Stroke thickness slider, and interactive Color Pickers.
2. **Optimization & Output (Center):**
   * Image format conversion and resize/compression constraints.
   * Radio buttons for Export Format (Images vs. PDFs).
   * Contextual hidden field for Custom PDF name.
   * Radio buttons for Destination routing.
   * Large, accented "Start Process" button and modern Progress Bar with dynamic text statuses.
3. **Preview & Queue (Right):**
   * **Top (75% height):** Large Live Preview area with dynamic Text Stroke generation and Excluded Image overlay.
   * **Bottom (25% height):** Dual ListBoxes (Excluded vs. Ready) with interactive transfer buttons, dark-mode scrollbars, and Drag & Drop enabled.

## 5. Development Roadmap
* **Phase 1: DONE** Build the shell (Custom XAML Dark Theme, Layout grids, Controls).
* **Phase 2: DONE** Implement File Management (Drag & Drop, Deep Search, Fast Dual ListBox logic, Keyboard bindings).
* **Phase 3: DONE** Implement Live Preview dynamic rendering, Real-time Stroke generation, Custom Dark MessageBox, and UI triggers.
* **Phase 4: DONE** Implement core Image Processing logic (`ImageSharp`, Format conversion, Size optimization, Watermarking).
* **Phase 5: DONE** Integrate PDF Engine (`QuestPDF`) for Single/Multipage exports, Export Routing, and App Settings Persistance.
* **Phase 6: DONE** Implement Async/Await for background processing and animate the Progress Bar (UI simulation complete, ready for backend hookup).
* **Phase 7: PENDING** Generate standalone portable `.exe` file with custom application icon.