-- ════════════════════════════════════════════════════════════════════════
-- Миграция со старой схемы (без пользователей) на новую (с авторизацией).
--
-- Используйте этот скрипт ВМЕСТО schema.sql, если у вас есть данные в БД
-- которые вы хотите сохранить.
--
-- Что делает:
--   1. Создаёт таблицы users и sessions (если их нет)
--   2. Создаёт admin'а если в users пусто (admin / Admin123!)
--   3. Добавляет колонку user_id в profiles, known_devices, backup_history
--   4. Привязывает все существующие данные к admin'у
--   5. Создаёт уникальные индексы в рамках пользователя
--
-- ВАЖНО: запустите этот скрипт один раз. Повторный запуск может упасть на
-- ALTER TABLE (колонка уже добавлена).
-- ════════════════════════════════════════════════════════════════════════

USE `usbcopier`;

-- ── 1. Таблицы пользователей ────────────────────────────────────────────
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

CREATE TABLE IF NOT EXISTS `sessions` (
    `token`       VARCHAR(64) NOT NULL,
    `user_id`     INT UNSIGNED NOT NULL,
    `created_at`  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `expires_at`  DATETIME NOT NULL,
    PRIMARY KEY (`token`),
    KEY `idx_sessions_user` (`user_id`),
    CONSTRAINT `fk_sessions_user`
        FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ── 2. admin создаётся приложением при старте, если users пуст,
--      поэтому в скрипте его не вставляем. После запуска API один раз
--      admin появится автоматически.

-- ── 3. Добавляем user_id в profiles, known_devices, backup_history ─────
-- MySQL не имеет ADD COLUMN IF NOT EXISTS до 8.0.29, поэтому пишем
-- так, чтобы при наличии колонки скрипт упал понятно — а не молча.
ALTER TABLE `profiles`        ADD COLUMN `user_id` INT UNSIGNED NULL AFTER `profile_id`;
ALTER TABLE `profiles`        ADD COLUMN `backup_mode` ENUM('NewVersion','UpdateLatest') NOT NULL DEFAULT 'NewVersion' AFTER `trigger_mode`;
ALTER TABLE `known_devices`   ADD COLUMN `user_id` INT UNSIGNED NULL AFTER `device_id`;
ALTER TABLE `backup_history`  ADD COLUMN `user_id` INT UNSIGNED NULL AFTER `history_id`;

-- ── 4. Привязываем существующие данные к admin'у ───────────────────────
-- ВАЖНО: сначала запустите API один раз чтобы admin создался,
-- потом выполните этот UPDATE.
UPDATE `profiles`
    SET `user_id` = (SELECT `user_id` FROM `users` WHERE `is_admin` = 1 LIMIT 1)
    WHERE `user_id` IS NULL;
UPDATE `known_devices`
    SET `user_id` = (SELECT `user_id` FROM `users` WHERE `is_admin` = 1 LIMIT 1)
    WHERE `user_id` IS NULL;
UPDATE `backup_history`
    SET `user_id` = (SELECT `user_id` FROM `users` WHERE `is_admin` = 1 LIMIT 1)
    WHERE `user_id` IS NULL;

-- ── 5. Делаем колонки NOT NULL и добавляем FK + уникальные индексы ─────
ALTER TABLE `profiles`       MODIFY COLUMN `user_id` INT UNSIGNED NOT NULL;
ALTER TABLE `known_devices`  MODIFY COLUMN `user_id` INT UNSIGNED NOT NULL;
ALTER TABLE `backup_history` MODIFY COLUMN `user_id` INT UNSIGNED NOT NULL;

-- Снимаем старые уникальные индексы (если есть)
ALTER TABLE `profiles`      DROP INDEX `uk_profiles_name`;
ALTER TABLE `known_devices` DROP INDEX `uk_known_devices_serial_label`;

-- Новые уникальные индексы — в рамках пользователя
ALTER TABLE `profiles`
    ADD UNIQUE KEY `uk_profiles_user_name` (`user_id`, `name`);
ALTER TABLE `known_devices`
    ADD UNIQUE KEY `uk_known_devices_user_serial_label` (`user_id`, `volume_serial`, `volume_label`);

-- Foreign keys
ALTER TABLE `profiles`
    ADD CONSTRAINT `fk_profiles_user`
    FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`)
    ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE `known_devices`
    ADD CONSTRAINT `fk_known_devices_user`
    FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`)
    ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE `backup_history`
    ADD CONSTRAINT `fk_backup_history_user`
    FOREIGN KEY (`user_id`) REFERENCES `users`(`user_id`)
    ON DELETE CASCADE ON UPDATE CASCADE;
