-- ════════════════════════════════════════════════════════════════════════
-- UsbCopier — schema for MySQL 8 / phpMyAdmin
--
-- ВНИМАНИЕ: этот скрипт ПОЛНОСТЬЮ ПЕРЕСОЗДАЁТ таблицы. Все данные
-- (профили, история, пользователи) будут УДАЛЕНЫ.
-- Если хотите сохранить существующие данные — используйте
-- migration_v2.sql вместо этого скрипта.
--
-- При первом запуске API после импорта схемы:
--   • Если в users нет admin'а — он создаётся автоматически
--     с логином admin / паролем Admin123!  (см. Program.cs)
-- ════════════════════════════════════════════════════════════════════════

CREATE DATABASE IF NOT EXISTS `usbcopier`
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_unicode_ci;

USE `usbcopier`;

-- ── Сначала чистим: явно удаляем таблицы в обратном порядке зависимостей ──
-- Без этого ALTER старых структур не подхватывает новые колонки (например, user_id)
-- и API падает с "Unknown column 'p.user_id'".
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS `backup_errors`;
DROP TABLE IF EXISTS `backup_history`;
DROP TABLE IF EXISTS `profile_schedule_times`;
DROP TABLE IF EXISTS `profile_extensions`;
DROP TABLE IF EXISTS `profile_categories`;
DROP TABLE IF EXISTS `known_devices`;
DROP TABLE IF EXISTS `profiles`;
DROP TABLE IF EXISTS `sessions`;
DROP TABLE IF EXISTS `users`;
SET FOREIGN_KEY_CHECKS = 1;

-- ── Пользователи ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `users` (
    `user_id`        INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `username`       VARCHAR(50)  NOT NULL,
    `email`          VARCHAR(200) NOT NULL,
    `password_hash`  VARCHAR(255) NOT NULL,
    `is_admin`       TINYINT(1)   NOT NULL DEFAULT 0,
    `created_at`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`user_id`),
    UNIQUE KEY `uk_users_username` (`username`),
    UNIQUE KEY `uk_users_email`    (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ── Сессии (токены доступа) ─────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `sessions` (
    `token`       VARCHAR(64) NOT NULL,
    `user_id`     INT UNSIGNED NOT NULL,
    `created_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `expires_at`  DATETIME NOT NULL,
    PRIMARY KEY (`token`),
    KEY `idx_sessions_user`    (`user_id`),
    KEY `idx_sessions_expires` (`expires_at`),
    CONSTRAINT `fk_sessions_user`
        FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ── Профили (теперь привязаны к user_id) ───────────────────────────────
CREATE TABLE IF NOT EXISTS `profiles` (
    `profile_id`         INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `user_id`            INT UNSIGNED NOT NULL,
    `name`               VARCHAR(100) NOT NULL,
    `destination_path`   VARCHAR(500) NOT NULL DEFAULT '',
    `include_subfolders` TINYINT(1)   NOT NULL DEFAULT 1,
    `grouping`           ENUM('Original','ByType','ByDate','BySize') NOT NULL DEFAULT 'Original',
    `date_granularity`   ENUM('Month','Year') NOT NULL DEFAULT 'Month',
    `trigger_mode`       ENUM('Manual','OnUsbConnect','Schedule') NOT NULL DEFAULT 'OnUsbConnect',
    `backup_mode`        ENUM('NewVersion','UpdateLatest') NOT NULL DEFAULT 'NewVersion',
    `every_n_hours`      INT NOT NULL DEFAULT 0,
    `custom_extensions`  VARCHAR(500) NOT NULL DEFAULT '',
    `created_at`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`profile_id`),
    UNIQUE KEY `uk_profiles_user_name` (`user_id`, `name`),
    KEY `idx_profiles_user` (`user_id`),
    CONSTRAINT `fk_profiles_user`
        FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `profile_categories` (
    `category_id`  INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `profile_id`   INT UNSIGNED NOT NULL,
    `name`         VARCHAR(50)  NOT NULL,
    `is_enabled`   TINYINT(1)   NOT NULL DEFAULT 1,
    `sort_order`   INT          NOT NULL DEFAULT 0,
    PRIMARY KEY (`category_id`),
    KEY `idx_profile_categories_profile` (`profile_id`),
    CONSTRAINT `fk_profile_categories_profile`
        FOREIGN KEY (`profile_id`) REFERENCES `profiles`(`profile_id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `profile_extensions` (
    `extension_id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `category_id`  INT UNSIGNED NOT NULL,
    `extension`    VARCHAR(20)  NOT NULL,
    `is_checked`   TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`extension_id`),
    KEY `idx_profile_extensions_category` (`category_id`),
    CONSTRAINT `fk_profile_extensions_category`
        FOREIGN KEY (`category_id`) REFERENCES `profile_categories`(`category_id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `profile_schedule_times` (
    `time_id`    INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `profile_id` INT UNSIGNED NOT NULL,
    `hh`         TINYINT UNSIGNED NOT NULL,
    `mm`         TINYINT UNSIGNED NOT NULL,
    PRIMARY KEY (`time_id`),
    KEY `idx_profile_schedule_profile` (`profile_id`),
    CONSTRAINT `fk_profile_schedule_profile`
        FOREIGN KEY (`profile_id`) REFERENCES `profiles`(`profile_id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ── Известные устройства (тоже под пользователя) ───────────────────────
CREATE TABLE IF NOT EXISTS `known_devices` (
    `device_id`     INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `user_id`       INT UNSIGNED NOT NULL,
    `volume_serial` VARCHAR(64)  DEFAULT NULL,
    `volume_label`  VARCHAR(100) NOT NULL,
    `file_system`   VARCHAR(20)  DEFAULT NULL,
    `total_bytes`   BIGINT       NOT NULL DEFAULT 0,
    `first_seen_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `last_seen_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`device_id`),
    UNIQUE KEY `uk_known_devices_user_serial_label` (`user_id`, `volume_serial`, `volume_label`),
    KEY `idx_known_devices_user` (`user_id`),
    CONSTRAINT `fk_known_devices_user`
        FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ── История бэкапов ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `backup_history` (
    `history_id`    INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `user_id`       INT UNSIGNED NOT NULL,
    `profile_id`    INT UNSIGNED DEFAULT NULL,
    `device_id`     INT UNSIGNED DEFAULT NULL,
    `trigger`       ENUM('Manual','AutoOnConnect','Schedule') NOT NULL,
    `status`        ENUM('Success','PartialErrors','Failed','Cancelled','NoFilesMatched') NOT NULL,
    `source_letter` VARCHAR(10)  NOT NULL,
    `source_label`  VARCHAR(100) NOT NULL,
    `target_folder` VARCHAR(500) NOT NULL,
    `files_copied`  INT NOT NULL DEFAULT 0,
    `files_failed`  INT NOT NULL DEFAULT 0,
    `error_message` VARCHAR(1000) DEFAULT NULL,
    `started_at`    DATETIME NOT NULL,
    `finished_at`   DATETIME NOT NULL,
    PRIMARY KEY (`history_id`),
    KEY `idx_backup_history_user`    (`user_id`),
    KEY `idx_backup_history_profile` (`profile_id`),
    KEY `idx_backup_history_device`  (`device_id`),
    KEY `idx_backup_history_started` (`started_at`),
    CONSTRAINT `fk_backup_history_user`
        FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_backup_history_profile`
        FOREIGN KEY (`profile_id`) REFERENCES `profiles`(`profile_id`)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `fk_backup_history_device`
        FOREIGN KEY (`device_id`)  REFERENCES `known_devices`(`device_id`)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `backup_errors` (
    `error_id`       INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `history_id`     INT UNSIGNED NOT NULL,
    `file_path`      VARCHAR(1000) NOT NULL,
    `error_message`  VARCHAR(1000) NOT NULL,
    PRIMARY KEY (`error_id`),
    KEY `idx_backup_errors_history` (`history_id`),
    CONSTRAINT `fk_backup_errors_history`
        FOREIGN KEY (`history_id`) REFERENCES `backup_history`(`history_id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
