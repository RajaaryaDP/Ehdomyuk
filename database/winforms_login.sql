-- Database schema untuk project WinFormsApp1
-- Jalankan file ini melalui MySQL/Laragon/phpMyAdmin.

CREATE DATABASE IF NOT EXISTS `winforms_login`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `winforms_login`;

-- 1. Tabel Dosen
CREATE TABLE IF NOT EXISTS `dosen` (
    `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `nidn` VARCHAR(20) NULL,
    `nama` VARCHAR(100) NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_dosen_nidn` (`nidn`)
) ENGINE=InnoDB;

-- 2. Tabel Users (Mendukung role mahasiswa, admin, dosen, nama lengkap, dan kelas)
CREATE TABLE IF NOT EXISTS `users` (
    `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `username` VARCHAR(50) NOT NULL,
    `nama` VARCHAR(100) NULL,
    `password` VARCHAR(255) NOT NULL,
    `role` ENUM('mahasiswa', 'admin', 'dosen') NOT NULL DEFAULT 'mahasiswa',
    `kelas` VARCHAR(20) NULL,
    `dosen_id` INT UNSIGNED NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_users_username` (`username`),
    KEY `idx_users_dosen_id` (`dosen_id`),
    CONSTRAINT `fk_users_dosen`
        FOREIGN KEY (`dosen_id`) REFERENCES `dosen` (`id`)
        ON UPDATE CASCADE
        ON DELETE SET NULL
) ENGINE=InnoDB;

-- 3. Tabel Mata Kuliah
CREATE TABLE IF NOT EXISTS `mata_kuliah` (
    `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `nama` VARCHAR(100) NOT NULL,
    `kelas` VARCHAR(20) NULL DEFAULT 'TI 3C',
    `ruang` VARCHAR(20) NULL,
    `dosen_id` INT UNSIGNED NOT NULL,
    PRIMARY KEY (`id`),
    KEY `idx_mata_kuliah_dosen_id` (`dosen_id`),
    CONSTRAINT `fk_mata_kuliah_dosen`
        FOREIGN KEY (`dosen_id`) REFERENCES `dosen` (`id`)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
) ENGINE=InnoDB;

-- 4. Tabel Hasil Pengisian EDOM (Dengan data statistik & nilai evaluasi)
CREATE TABLE IF NOT EXISTS `edom_pengisian` (
    `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `username` VARCHAR(50) NOT NULL,
    `mata_kuliah_id` INT UNSIGNED NOT NULL,
    `skor_pedagogik` DECIMAL(3,2) NULL DEFAULT 4.50,
    `skor_profesional` DECIMAL(3,2) NULL DEFAULT 4.50,
    `skor_kepribadian` DECIMAL(3,2) NULL DEFAULT 4.50,
    `skor_sosial` DECIMAL(3,2) NULL DEFAULT 4.50,
    `nilai_rata_rata` DECIMAL(3,2) NULL DEFAULT 4.50,
    `saran` TEXT NULL,
    `diisi_pada` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_pengisian` (`username`, `mata_kuliah_id`),
    KEY `idx_pengisian_mata_kuliah_id` (`mata_kuliah_id`),
    CONSTRAINT `fk_pengisian_mata_kuliah`
        FOREIGN KEY (`mata_kuliah_id`) REFERENCES `mata_kuliah` (`id`)
        ON UPDATE CASCADE
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- 5. Tabel Butir Pertanyaan EDOM (INSTRUMEN EDOM 2023, skala 1-5)
CREATE TABLE IF NOT EXISTS `edom_pertanyaan` (
    `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `nomor` INT NOT NULL,
    `kompetensi` VARCHAR(20) NOT NULL COMMENT 'pedagogik | profesional | kepribadian | sosial',
    `pertanyaan` VARCHAR(255) NOT NULL,
    `aktif` TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_pertanyaan_nomor` (`nomor`)
) ENGINE=InnoDB;

-- 6. Tabel Jawaban per Butir (satu baris per butir per pengisian)
CREATE TABLE IF NOT EXISTS `edom_jawaban` (
    `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `pengisian_id` INT UNSIGNED NOT NULL,
    `pertanyaan_id` INT UNSIGNED NOT NULL,
    `nilai` TINYINT NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_jawaban` (`pengisian_id`, `pertanyaan_id`),
    KEY `idx_jawaban_pertanyaan_id` (`pertanyaan_id`),
    CONSTRAINT `fk_jawaban_pengisian`
        FOREIGN KEY (`pengisian_id`) REFERENCES `edom_pengisian` (`id`)
        ON DELETE CASCADE,
    CONSTRAINT `fk_jawaban_pertanyaan`
        FOREIGN KEY (`pertanyaan_id`) REFERENCES `edom_pertanyaan` (`id`)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ============================================================================
-- DATA DOSEN KELAS TI 3C (NIP/NIDN dikosongkan sementara)
-- ============================================================================
INSERT INTO `dosen` (`nidn`, `nama`)
SELECT NULL, src.`nama`
FROM (
    SELECT 'Anggi Mardiyono' AS `nama`
    UNION ALL SELECT 'Anita Hidayati'
    UNION ALL SELECT 'Asep Taufik Muharram'
    UNION ALL SELECT 'Bambang Warsuta'
    UNION ALL SELECT 'Chairul Fikri'
    UNION ALL SELECT 'Defiana Arnaldy'
    UNION ALL SELECT 'Fajar Septian'
    UNION ALL SELECT 'Iwan Sonjaya'
    UNION ALL SELECT 'Risna Sari'
    UNION ALL SELECT 'Rizki Elisa Nalawati'
) src
WHERE NOT EXISTS (SELECT 1 FROM `dosen` d WHERE d.`nama` = src.`nama`);

-- ============================================================================
-- DATA MATA KULIAH KELAS TI 3C
-- ============================================================================
INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK309 - Grafika Komputer', 'TI 3C', 'AA 301', `id`
FROM `dosen` WHERE `nama` = 'Anggi Mardiyono'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK309 - Grafika Komputer');

INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK308 - Analisis dan Desain Sistem Informasi', 'TI 3C', 'AA 302', `id`
FROM `dosen` WHERE `nama` = 'Anita Hidayati'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK308 - Analisis dan Desain Sistem Informasi');

INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK306 - Pemrograman Web Lanjut', 'TI 3C', 'Lab RPL 1', `id`
FROM `dosen` WHERE `nama` = 'Asep Taufik Muharram'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK306 - Pemrograman Web Lanjut');

INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK302 - Manajemen Proyek TIK', 'TI 3C', 'GSG 201', `id`
FROM `dosen` WHERE `nama` = 'Bambang Warsuta'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK302 - Manajemen Proyek TIK');

INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK301 - Pemrograman Visual', 'TI 3C', 'Lab RPL 2', `id`
FROM `dosen` WHERE `nama` = 'Chairul Fikri'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK301 - Pemrograman Visual');

INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK310 - Sistem Terdistribusi', 'TI 3C', 'AA 304', `id`
FROM `dosen` WHERE `nama` = 'Defiana Arnaldy'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK310 - Sistem Terdistribusi');

INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK305 - Metode Numerik', 'TI 3C', 'PUT 301', `id`
FROM `dosen` WHERE `nama` = 'Fajar Septian'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK305 - Metode Numerik');

INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK303 - Desain UI/UX', 'TI 3C', 'Lab Multimedia', `id`
FROM `dosen` WHERE `nama` = 'Iwan Sonjaya'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK303 - Desain UI/UX');

INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK307 - Pemrograman Basis Data', 'TI 3C', 'GSG 204', `id`
FROM `dosen` WHERE `nama` = 'Risna Sari'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK307 - Pemrograman Basis Data');

INSERT INTO `mata_kuliah` (`nama`, `kelas`, `ruang`, `dosen_id`)
SELECT 'TIK304 - Kecerdasan Buatan', 'TI 3C', 'AA 205', `id`
FROM `dosen` WHERE `nama` = 'Rizki Elisa Nalawati'
AND NOT EXISTS (SELECT 1 FROM `mata_kuliah` WHERE `nama` = 'TIK304 - Kecerdasan Buatan');

-- ============================================================================
-- BUTIR PERTANYAAN EDOM (sesuai INSTRUMEN EDOM 2023)
-- Skala: 1 (Sangat Kurang), 2 (Kurang), 3 (Cukup), 4 (Baik), 5 (Sangat Baik)
-- ============================================================================
INSERT INTO `edom_pertanyaan` (`nomor`, `kompetensi`, `pertanyaan`)
VALUES
    -- A. Kompetensi Pedagogik
    (1,  'pedagogik',   'Kesiapan memberikan kuliah dan/atau praktek/praktikum'),
    (2,  'pedagogik',   'Keteraturan dan ketertiban penyelenggaraan perkuliahan'),
    (3,  'pedagogik',   'Kemampuan menghidupkan suasana kelas'),
    (4,  'pedagogik',   'Kejelasan penyampaian materi dan jawaban terhadap pertanyaan di kelas'),
    (5,  'pedagogik',   'Pemanfaatan media dan teknologi pembelajaran'),
    (6,  'pedagogik',   'Keanekaragaman cara pengukuran/penilaian hasil belajar'),
    (7,  'pedagogik',   'Pemberian umpan balik terhadap tugas/penilaian'),
    (8,  'pedagogik',   'Kesesuaian materi ujian dan/atau tugas dengan tujuan mata kuliah'),
    (9,  'pedagogik',   'Kesesuaian nilai yang diberikan dengan hasil belajar'),
    -- B. Kompetensi Profesional
    (10, 'profesional', 'Kemampuan menjelaskan pokok bahasan/topik secara tepat sesuai Garis-garis Besar Pengajaran (GBPP) dan Satuan Acara Pengajaran (SAP)'),
    (11, 'profesional', 'Kemampuan memberi contoh relevan dari konsep yang diajarkan berdasarkan Program Studi'),
    (12, 'profesional', 'Kemampuan menjelaskan keterkaitan bidang/topik yang diajarkan dengan bidang/topik lain'),
    (13, 'profesional', 'Kemampuan menjelaskan keterkaitan bidang/topik yang diajarkan dengan konteks kehidupan'),
    (14, 'profesional', 'Penguasaan akan isu-isu mutakhir dalam bidang yang diajarkan (kemutakhiran bahan/referensi kuliah)'),
    (15, 'profesional', 'Penggunaan hasil-hasil penelitian untuk meningkatkan kualitas perkuliahan'),
    (16, 'profesional', 'Pelibatan mahasiswa dalam penelitian/kajian dan atau pengembangan/rekayasa/desain yang dilakukan dosen'),
    (17, 'profesional', 'Kemampuan menggunakan beragam teknologi komunikasi'),
    -- C. Kompetensi Kepribadian
    (18, 'kepribadian', 'Kewibawaan sebagai pribadi dosen'),
    (19, 'kepribadian', 'Kearifan dalam mengambil keputusan'),
    (20, 'kepribadian', 'Menjadi contoh dalam bersikap dan berperilaku'),
    (21, 'kepribadian', 'Satu kata dan tindakan'),
    (22, 'kepribadian', 'Kemampuan mengendalikan diri dalam berbagai situasi dan kondisi'),
    (23, 'kepribadian', 'Adil dalam memperlakukan mahasiswa'),
    -- D. Kompetensi Sosial
    (24, 'sosial',      'Kemampuan menyampaikan pendapat'),
    (25, 'sosial',      'Kemampuan menerima kritik, saran, dan pendapat dari mahasiswa'),
    (26, 'sosial',      'Mengenal dengan baik mahasiswa yang mengikuti kuliahnya'),
    (27, 'sosial',      'Toleransi terhadap keberagaman mahasiswa')
ON DUPLICATE KEY UPDATE
    `kompetensi` = VALUES(`kompetensi`),
    `pertanyaan` = VALUES(`pertanyaan`);

-- ============================================================================
-- DATA AKUN PENGGUNA (Admin, 5 Mahasiswa TI 3C, dan 1 Akun Dosen)
-- Password default: admin123 (admin), 12345 (semua mahasiswa), dosen123 (dosen)
-- ============================================================================
-- Akun Admin
INSERT INTO `users` (`username`, `nama`, `password`, `role`, `kelas`, `dosen_id`)
VALUES ('admin', 'Administrator', 'admin123', 'admin', NULL, NULL)
ON DUPLICATE KEY UPDATE `nama` = VALUES(`nama`), `role` = VALUES(`role`);

-- 5 Akun Mahasiswa Kelas TI 3C (Password sama: 12345)
INSERT INTO `users` (`username`, `nama`, `password`, `role`, `kelas`, `dosen_id`)
VALUES
    ('2507411027', 'Arya Dwi Prasetyo',       '12345', 'mahasiswa', 'TI 3C', NULL),
    ('2507411086', 'Arraya Andika',           '12345', 'mahasiswa', 'TI 3C', NULL),
    ('2507411026', 'Deni Dwi Prasetya',       '12345', 'mahasiswa', 'TI 3C', NULL),
    ('2507411023', 'Fadhilah Ravid Baladewa', '12345', 'mahasiswa', 'TI 3C', NULL),
    ('2507411029', 'Raja Arya Darma Putra',   '12345', 'mahasiswa', 'TI 3C', NULL)
ON DUPLICATE KEY UPDATE
    `nama` = VALUES(`nama`),
    `password` = VALUES(`password`),
    `role` = VALUES(`role`),
    `kelas` = VALUES(`kelas`);

-- 1 Akun Dosen untuk login (terhubung ke Asep Taufik Muharram)
INSERT INTO `users` (`username`, `nama`, `password`, `role`, `kelas`, `dosen_id`)
SELECT 'dosen', 'Asep Taufik Muharram', 'dosen123', 'dosen', NULL, `id`
FROM `dosen` WHERE `nama` = 'Asep Taufik Muharram'
ON DUPLICATE KEY UPDATE
    `nama` = VALUES(`nama`),
    `role` = VALUES(`role`),
    `dosen_id` = VALUES(`dosen_id`);

-- ============================================================================
-- DATA CONTOH HASIL PENGISIAN EDOM
-- ============================================================================
INSERT INTO `edom_pengisian` (`username`, `mata_kuliah_id`, `skor_pedagogik`, `skor_profesional`, `skor_kepribadian`, `skor_sosial`, `nilai_rata_rata`, `saran`, `diisi_pada`)
SELECT '2507411026', mk.id, 4.80, 4.90, 5.00, 4.70, 4.85, 'Penyampaian materi sangat interaktif dan mudah dipahami. Contoh coding langsung relevan dengan industri.', NOW() - INTERVAL 3 DAY
FROM `mata_kuliah` mk WHERE mk.nama = 'TIK306 - Pemrograman Web Lanjut'
ON DUPLICATE KEY UPDATE `nilai_rata_rata` = VALUES(`nilai_rata_rata`);

INSERT INTO `edom_pengisian` (`username`, `mata_kuliah_id`, `skor_pedagogik`, `skor_profesional`, `skor_kepribadian`, `skor_sosial`, `nilai_rata_rata`, `saran`, `diisi_pada`)
SELECT '2507411027', mk.id, 4.50, 4.60, 4.70, 4.60, 4.60, 'Dosen sangat ramah dan sabar membimbing saat mahasiswa mengalami error saat praktikum.', NOW() - INTERVAL 2 DAY
FROM `mata_kuliah` mk WHERE mk.nama = 'TIK306 - Pemrograman Web Lanjut'
ON DUPLICATE KEY UPDATE `nilai_rata_rata` = VALUES(`nilai_rata_rata`);

INSERT INTO `edom_pengisian` (`username`, `mata_kuliah_id`, `skor_pedagogik`, `skor_profesional`, `skor_kepribadian`, `skor_sosial`, `nilai_rata_rata`, `saran`, `diisi_pada`)
SELECT '2507411029', mk.id, 4.60, 4.70, 4.80, 4.50, 4.65, 'Modul dan slide presentasi lengkap, penjelasan tugas terstruktur dengan baik.', NOW() - INTERVAL 1 DAY
FROM `mata_kuliah` mk WHERE mk.nama = 'TIK306 - Pemrograman Web Lanjut'
ON DUPLICATE KEY UPDATE `nilai_rata_rata` = VALUES(`nilai_rata_rata`);

INSERT INTO `edom_pengisian` (`username`, `mata_kuliah_id`, `skor_pedagogik`, `skor_profesional`, `skor_kepribadian`, `skor_sosial`, `nilai_rata_rata`, `saran`, `diisi_pada`)
SELECT '2507411026', mk.id, 4.70, 4.80, 4.60, 4.70, 4.70, 'Latihan query SQL dan normalisasi dijelaskan tahap demi tahap.', NOW() - INTERVAL 2 DAY
FROM `mata_kuliah` mk WHERE mk.nama = 'TIK307 - Pemrograman Basis Data'
ON DUPLICATE KEY UPDATE `nilai_rata_rata` = VALUES(`nilai_rata_rata`);
