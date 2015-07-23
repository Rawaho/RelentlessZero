CREATE TABLE IF NOT EXISTS `account_avatar` (
    `id` int(10) unsigned NOT NULL DEFAULT '0',
    `head` tinyint(4) unsigned NOT NULL DEFAULT '0',
    `body` tinyint(4) unsigned NOT NULL DEFAULT '0',
    `leg` tinyint(4) unsigned NOT NULL DEFAULT '0',
    `armBack` tinyint(4) unsigned NOT NULL DEFAULT '0',
    `armFront` tinyint(4) unsigned NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

INSERT INTO `account_avatar` VALUES
    ('1', '33', '10', '41', '4', '15');