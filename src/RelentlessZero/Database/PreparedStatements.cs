/*
 * Copyright (C) 2013-2016 RelentlessZero
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using MySql.Data.MySqlClient;

namespace RelentlessZero.Database
{
    public enum PreparedStatement
    {
        AccountInsert,
        AccountSelect,
        AccountSelectAll,
        AccountUpdate,
        AvatarInsert,
        AvatarSelect,
        AvatarSelectAll,
        AvatarTemplateSelect,
        BanDelete,
        BanSelect,
        DeckDelete,
        DeckInsert,
        DeckMax,
        DeckScrollDelete,
        DeckScrollInsert,
        DeckScrollSelect,
        DeckSelect,
        ScrollAbilityTemplateSelect,
        ScrollInsert,
        ScrollMax,
        ScrollPassiveTemplateSelect,
        ScrollSelect,
        ScrollSelectAll,
        ScrollTagTemplateSelect,
        ScrollTemplateAbilitySelect,
        ScrollTemplatePassiveSelect,
        ScrollTemplateSelect,
        ScrollTemplateSubTypeSelect,
        ScrollTemplateTagSelect
    }

    public static partial class DatabaseManager
    {
        private static void InitialisePreparedStatements()
        {
            AddPreparedStatement(PreparedStatement.AccountSelect, "SELECT id, password, salt, adminRole, gold, shards, rating, flags FROM account_info WHERE username = ?;", MySqlDbType.VarChar);
            AddPreparedStatement(PreparedStatement.AccountUpdate, "UPDATE `account_info` SET `gold` = ?, `shards` = ?, `rating` = ?, `flags` = ?", MySqlDbType.UInt32, MySqlDbType.UInt32, MySqlDbType.UInt16, MySqlDbType.UInt16);
            AddPreparedStatement(PreparedStatement.AccountInsert, "INSERT INTO `account_info` (`id`, `username`, `password`, `salt`, `adminRole`) VALUES (?, ?, ?, ?, ?);",
                MySqlDbType.UInt32, MySqlDbType.VarChar, MySqlDbType.VarChar, MySqlDbType.VarChar, MySqlDbType.UByte);

            AddPreparedStatement(PreparedStatement.BanSelect, "SELECT id, timestamp FROM account_ban WHERE id = ?;", MySqlDbType.UInt32);
            AddPreparedStatement(PreparedStatement.BanDelete, "DELETE FROM account_ban WHERE id = ?;", MySqlDbType.UInt32);

            AddPreparedStatement(PreparedStatement.AvatarSelect, "SELECT `head`, `body`, `leg`, `armBack`, `armFront` FROM `account_avatar` WHERE `id` = ?;", MySqlDbType.UInt32);
            AddPreparedStatement(PreparedStatement.AvatarInsert, "INSERT INTO `account_avatar` (`id`, `head`, `body`, `leg`, `armBack`, `armFront`) VALUES(?, ?, ?, ?, ?, ?) " +
                "ON DUPLICATE KEY UPDATE `head` = VALUES(`head`), `body` = VALUES(`body`), `leg` = VALUES(`leg`), `armBack` = VALUES(`armBack`), `armFront` = VALUES(`armFront`);",
                MySqlDbType.UInt32, MySqlDbType.UInt16, MySqlDbType.UInt16, MySqlDbType.UInt16, MySqlDbType.UInt16, MySqlDbType.UInt16);

            AddPreparedStatement(PreparedStatement.ScrollSelect, "SELECT `id`, `scrollEntry`, `level`, `timestamp`, `damage`, `destroyed`, `heal`, `idolKills`, `played`, `sacrificed`,"
                + "`totalGames`, `unitKills`, `wins`, `tradable` FROM `scroll_instance` WHERE `accountId` = ?;", MySqlDbType.UInt32);
            AddPreparedStatement(PreparedStatement.ScrollInsert, "INSERT INTO `scroll_instance` (`id`, `accountId`, `scrollEntry`, `level`, `timestamp`, `damage`, `destroyed`, `heal`, " +
                "`idolKills`, `played`, `sacrificed`, `totalGames`, `unitKills`, `wins`, `tradable`) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?) " +
                "ON DUPLICATE KEY UPDATE `id` = VALUES(`id`), `accountId` = VALUES(`accountId`), `scrollEntry` = VALUES(`scrollEntry`), " +
                "`level` = VALUES(`level`), `timestamp` = VALUES(`timestamp`), `damage` = VALUES(`damage`), `destroyed` = VALUES (`destroyed`), " +
                "`heal` = VALUES(`heal`), `idolKills` = VALUES(`idolKills`), `played` = VALUES(`played`), `sacrificed` = VALUES (`sacrificed`), " +
                "`totalGames` = VALUES(`totalGames`), `unitKills` = VALUES(`unitKills`), `wins` = VALUES(`wins`), `tradable` = VALUES(`tradable`);",
                MySqlDbType.UInt64, MySqlDbType.UInt32, MySqlDbType.UInt16, MySqlDbType.UByte, MySqlDbType.Int64, MySqlDbType.UInt32, MySqlDbType.UInt32,
                MySqlDbType.UInt32, MySqlDbType.UInt32, MySqlDbType.UInt32, MySqlDbType.UInt32, MySqlDbType.UInt32, MySqlDbType.UInt32, MySqlDbType.UInt32, MySqlDbType.UByte);

            AddPreparedStatement(PreparedStatement.DeckSelect, "SELECT `id`, `name`, `timestamp`, `flags` FROM `account_deck` WHERE `accountId` = ?;", MySqlDbType.UInt32);
            AddPreparedStatement(PreparedStatement.DeckInsert, "INSERT INTO `account_deck` (`id`, `accountId`, `name`, `timestamp`, `flags`) VALUES(?, ?, ?, ?, ?) " +
                "ON DUPLICATE KEY UPDATE `name` = VALUES(`name`), `timestamp` = VALUES(`timestamp`), `flags` = VALUES(`flags`);",
                MySqlDbType.UInt32, MySqlDbType.UInt32, MySqlDbType.VarChar, MySqlDbType.UInt64, MySqlDbType.UByte);
            AddPreparedStatement(PreparedStatement.DeckDelete, "DELETE FROM `account_deck` WHERE `id` = ? AND `accountId` = ?;", MySqlDbType.UInt32, MySqlDbType.UInt32);

            AddPreparedStatement(PreparedStatement.DeckScrollSelect, "SELECT `scrollInstance` FROM `account_deck_scroll` WHERE `id` = ?;", MySqlDbType.UInt32);
            AddPreparedStatement(PreparedStatement.DeckScrollDelete, "DELETE FROM `account_deck_scroll` WHERE `id` = ?;", MySqlDbType.UInt32);
            AddPreparedStatement(PreparedStatement.DeckScrollInsert, "INSERT INTO `account_deck_scroll` (`id`, `accountId`, `scrollInstance`) VALUES(?, ?, ?);", MySqlDbType.UInt32, MySqlDbType.UInt32, MySqlDbType.UInt64);

            AddPreparedStatement(PreparedStatement.AvatarTemplateSelect, "SELECT `entry`, `type`, `part`, `filename`, `set` FROM `avatar_part_template`;");
            AddPreparedStatement(PreparedStatement.ScrollPassiveTemplateSelect, "SELECT `entry`, `name`, `description` FROM `scroll_passive_template`;");
            AddPreparedStatement(PreparedStatement.ScrollAbilityTemplateSelect, "SELECT `entry`, `id`, `name`, `description`, `resource`, `cost` FROM `scroll_ability_template`;");
            AddPreparedStatement(PreparedStatement.ScrollTagTemplateSelect, "SELECT `entry`, `name`, `type` FROM `scroll_tag_template`;");
            AddPreparedStatement(PreparedStatement.ScrollTemplateSelect, "SELECT * FROM `scroll_template`;");
            AddPreparedStatement(PreparedStatement.ScrollTemplateSubTypeSelect, "SELECT `subType` FROM `scroll_template_subtype` WHERE `entry` = ?;", MySqlDbType.UInt16);
            AddPreparedStatement(PreparedStatement.ScrollTemplateTagSelect, "SELECT `tagEntry`, `value` FROM `scroll_template_tag` WHERE `entry` = ?;", MySqlDbType.UInt16);
            AddPreparedStatement(PreparedStatement.ScrollTemplateAbilitySelect, "SELECT `abilityEntry` FROM `scroll_template_ability` WHERE `entry` = ?;", MySqlDbType.UInt16);
            AddPreparedStatement(PreparedStatement.ScrollTemplatePassiveSelect, "SELECT `passiveEntry` FROM `scroll_template_passive` WHERE `entry` = ?;", MySqlDbType.UInt16);

            AddPreparedStatement(PreparedStatement.ScrollMax, "SELECT MAX(`id`) FROM `scroll_instance`;");
            AddPreparedStatement(PreparedStatement.DeckMax, "SELECT MAX(`id`) FROM `account_deck`;");

            AddPreparedStatement(PreparedStatement.AccountSelectAll, "SELECT `id`, `username`, `adminRole`, `gold`, `shards`, `rating` FROM `account_info`;");
            AddPreparedStatement(PreparedStatement.AvatarSelectAll, "SELECT `id`, `head`, `body`, `leg`, `armBack`, `armFront` FROM `account_avatar`;");
            AddPreparedStatement(PreparedStatement.ScrollSelectAll, "SELECT `accountId`, `id`, `scrollEntry` FROM `scroll_instance`;");
        }
    }
}
