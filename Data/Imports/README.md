# Folder Import Dataset (JSONL / TXT / CSV / XLSX)

Direktori ini digunakan untuk meletakkan dan mengimpor file dataset multi-format ke dalam ekosistem **dagangOnline**:

## 📁 Format yang Didukung

1. **`.jsonl` (JSON Lines / `.jsonlines`)**:
   - Satu baris mewakili satu record JSON utuh.
   - Sangat optimal untuk RAG Knowledge Chunks, fine-tuning data, permodelan Knowledge Graph ekonomi, dan katalog produk.
   - *Contoh format RAG:*
     ```json
     {"title": "Standar Pembayaran COD", "content": "Metode COD berlaku untuk area Jabodetabek dan Bandung Raya dengan batas maksimal transaksi Rp 2.500.000.", "category": "Logistik", "heading": "Ketentuan Transaksi"}
     {"title": "Program Subsidi Ongkir", "content": "Mitra UMKM terverifikasi berhak memperoleh potongan ongkos kirim hingga 40% setiap pengiriman antar-kota.", "category": "Logistik"}
     ```
   - *Contoh format Indikator Ekonomi:*
     ```json
     {"id": "INFLATION_CPI", "label": "Indeks Harga Konsumen", "category": "Macroeconomic", "currentValue": 2.85, "unit": "% YoY", "volatility30d": 0.12, "sentimentScore": -0.15}
     ```

2. **`.txt` (Text Corpus / Plain Text / Markdown)**:
   - Dokumen teks bebas, panduan operasional, regulasi UMKM, atau FAQ tertulis.
   - Teks secara otomatis di-chunking per paragraf (pemisah baris ganda) dan disimpan ke `KnowledgeDocument` & `KnowledgeChunk` sehingga langsung siap dicari oleh mesin RAG Hybrid Retrieval AI.
   - Judul dokumen otomatis diambil dari baris pertama (jika diawali `# `) atau dari nama file.

3. **`.csv` (Comma/Semicolon Separated Values)**:
   - File tabel data tabular dengan baris header.
   - Mendukung data indikator ekonomi, harga komoditas historis, dan katalog produk.

4. **`.xlsx` / `.xls` (Microsoft Excel Spreadsheet)**:
   - Spreadsheet OpenXML. Sheet pertama secara otomatis dibaca dan diindeks.

---

## 🚀 Cara Mengimpor Dataset

1. **Melalui Admin Dashboard**:
   - Masuk ke menu **Admin Dashboard -> Import Dataset**.
   - Unggah file `.jsonl`, `.txt`, `.csv`, atau `.xlsx` melalui form unggah.
   - Pilih target ingest (Auto-Detect, Knowledge Base RAG, Indikator Ekonomi, atau Katalog Produk).
2. **Melalui Folder Lokal (`Data/Imports/`)**:
   - Letakkan file dataset Anda di folder ini (`Data/Imports/nama_file.jsonl` atau `.txt`).
   - Tekan tombol **"⚡ Proses Seluruh File Lokal"** pada panel Admin Dashboard, atau panggil API `POST /api/v1/dataset/import-local`.
3. **Melalui REST API**:
   - Endpoint: `POST /api/v1/dataset/upload` (multipart/form-data)
   - Metadata endpoint: `GET /api/v1/dataset/supported-formats`
