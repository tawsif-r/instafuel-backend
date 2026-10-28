-- Run once on databases created before EFCore.NamingConventions was enabled.
-- This preserves all existing migration records and changes only column names.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = '__EFMigrationsHistory'
          AND column_name = 'MigrationId'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = '__EFMigrationsHistory'
          AND column_name = 'migration_id'
    ) THEN
        ALTER TABLE "__EFMigrationsHistory"
            RENAME COLUMN "MigrationId" TO migration_id;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = '__EFMigrationsHistory'
          AND column_name = 'ProductVersion'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = '__EFMigrationsHistory'
          AND column_name = 'product_version'
    ) THEN
        ALTER TABLE "__EFMigrationsHistory"
            RENAME COLUMN "ProductVersion" TO product_version;
    END IF;
END $$;
