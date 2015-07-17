CREATE TABLE IF NOT EXISTS `account_deck` (
    `id` int(10) unsigned NOT NULL DEFAULT '0',
    `accountId` int(10) unsigned NOT NULL DEFAULT '0',
    `name` varchar(50) NOT NULL DEFAULT '',
    `timestamp` bigint(20) unsigned NOT NULL DEFAULT '0',
    `flags` tinyint(3) unsigned NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`,`accountId`)
);

CREATE TABLE IF NOT EXISTS `account_deck_scroll` (
    `id` int(10) unsigned NOT NULL DEFAULT '0',
    `accountId` int(10) unsigned NOT NULL DEFAULT '0',
    `scrollInstance` int(10) unsigned NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`,`scrollInstance`,`accountId`),
    KEY `FK__account_deck_scroll__account_deck` (`id`),
    CONSTRAINT `FK__account_deck_scroll__account_deck` FOREIGN KEY (`id`) REFERENCES `account_deck` (`id`) ON DELETE CASCADE
);
