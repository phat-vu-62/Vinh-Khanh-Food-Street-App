START TRANSACTION;
ALTER TABLE "UserHistories" ADD "QRCode" text;

ALTER TABLE "Pois" ADD "ImageUrl" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260408072701_AddQRCodeToUserHistory', '10.0.0');

COMMIT;

