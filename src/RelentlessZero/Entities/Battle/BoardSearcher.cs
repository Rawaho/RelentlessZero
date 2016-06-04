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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RelentlessZero.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RelentlessZero.Entities
{
    public struct BoardSearcherSide
    {
        public TileColour Colour { get; }
        public List<Unit> Board { get; }

        public BoardSearcherSide(BattleSide battleSide)
        {
            Colour = battleSide.Colour;
            Board  = new List<Unit>(battleSide.Board);
        }
    }

    public class BoardSearcherTile
    {
        [JsonProperty(PropertyName = "color")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TileColour Colour { get; }
        [JsonProperty(PropertyName = "position")]
        public string Position { get; }

        [JsonIgnore()]
        public byte PositionX { get; }
        [JsonIgnore()]
        public byte PositionY { get; }

        public BoardSearcherTile(TileColour colour, byte positionX, byte positionY)
        {
            Colour    = colour;
            Position  = string.Format($"{positionY},{positionX}");
            PositionX = positionX;
            PositionY = positionY;
        }
    }

    public static class BoardSearcher
    {
        public const byte BoardWidth = 3;
        public const byte BoardLength = 5;

        private static Dictionary<TileSearchType, TileSearcher> tileSearchers;
        private static Dictionary<ushort, TileSearchType> scrollSearchTypes;

        delegate void TileSearcher(BoardSearcherSide self, BoardSearcherSide opponent, List<BoardSearcherTile> selectedTiles);

        public static void Initialise()
        {
            var startTime = DateTime.Now;

            DefineTileSearchers();
            DefineScrollSearchTypes();

            LogManager.Write("Board Searcher", $"Loaded {tileSearchers.Count} tile searcher(s) assigned to {scrollSearchTypes.Count} scroll(s) in "
                + $"{(DateTime.Now - startTime).Milliseconds} milisecond(s)!");
        }

        private static void DefineTileSearchers()
        {
            tileSearchers = new Dictionary<TileSearchType, TileSearcher>();

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                foreach (var methodInfo in type.GetMethods())
                    foreach (var tileSearcherAttribute in methodInfo.GetCustomAttributes<TileSearcherAttribute>())
                        tileSearchers[tileSearcherAttribute.SearchType] = (TileSearcher)Delegate.CreateDelegate(typeof(TileSearcher), methodInfo);
        }

        private static void DefineScrollSearchTypes()
        {
            scrollSearchTypes = new Dictionary<ushort, TileSearchType>();

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                foreach (var scrollAttribute in type.GetCustomAttributes<ScrollAttribute>())
                    scrollSearchTypes[scrollAttribute.ScrollEntry] = scrollAttribute.SearchType;
        }

        public static List<BoardSearcherTile> Search(ScrollInstance scrollInstance, BoardSearcherSide self, BoardSearcherSide opponent)
        {
            var selectType = TileSearchType.None;
            if (!scrollSearchTypes.ContainsKey(scrollInstance.Scroll.Entry))
            {
                // scroll has no predefined tile search type, use default depending on scroll kind
                switch (scrollInstance.Scroll.Kind)
                {
                    case ScrollKind.SPELL:
                    case ScrollKind.ENCHANTMENT:
                        selectType = TileSearchType.Unit;
                        break;
                    case ScrollKind.CREATURE:
                    case ScrollKind.STRUCTURE:
                        selectType = TileSearchType.TileSelf;
                        break;
                    default:
                        break;
                }
            }
            else
                selectType = scrollSearchTypes[scrollInstance.Scroll.Entry];

            var selectedTiles = new List<BoardSearcherTile>();
            tileSearchers[selectType].Invoke(self, opponent, selectedTiles);
            return selectedTiles;
        }

        public static bool ParsePosition(string position, out TileColour colour, out byte positionX, out byte positionY)
        {
            colour    = TileColour.unknown;
            positionX = 0;
            positionY = 0;

            if (string.IsNullOrEmpty(position))
                return false;

            var exploded = position.Split(',');
            if (exploded.Length != 3)
                return false;

            switch (exploded[0])
            {
                case "b":
                case "B":
                    colour = TileColour.black;
                    break;
                case "w":
                case "W":
                    colour = TileColour.white;
                    break;
                default:
                    return false;
            }

            if (!byte.TryParse(exploded[1], out positionY))
                return false;

            if (!byte.TryParse(exploded[2], out positionX))
                return false;

            if (positionX >= BoardWidth || positionY >= BoardLength)
                return false;

            return true;
        }

        public static bool IsTileOccupied(List<Unit> board, byte positionX, byte positionY)
        {
            return board.Exists(unit => unit.PositionX == positionX && unit.PositionY == positionY);
        }

        public static bool IsTileOccupied(List<BoardSearcherTile> tiles, TileColour colour, byte positionX, byte positionY)
        {
            return tiles.Exists(tile => tile.Colour == colour && tile.PositionX == positionX && tile.PositionY == positionY);
        }

        private static void SearchTilesFree(BoardSearcherSide side, List<BoardSearcherTile> selectedTiles)
        {
            bool[,] takenTiles = new bool[BoardWidth, BoardLength];

            foreach (var unit in side.Board)
                takenTiles[unit.PositionX, unit.PositionY] = true;

            for (byte x = 0; x < BoardWidth; x++)
                for (byte y = 0; y < BoardLength; y++)
                    if (!takenTiles[x, y])
                        selectedTiles.Add(new BoardSearcherTile(side.Colour, x, y));
        }

        private static void SearchTilesOccupied(BoardSearcherSide side, List<BoardSearcherTile> selectedTiles, ushort entry = 0)
        {
            foreach (var unit in side.Board)
                if (entry == 0 || entry == unit.Template.Entry)
                    selectedTiles.Add(new BoardSearcherTile(side.Colour, unit.PositionX, unit.PositionY));
        }

        [TileSearcher(TileSearchType.Tile)]
        public static void SelectTile(BoardSearcherSide self, BoardSearcherSide opponent, List<BoardSearcherTile> selectedTiles)
        {
            SearchTilesFree(self, selectedTiles);
            SearchTilesFree(opponent, selectedTiles);
        }

        [TileSearcher(TileSearchType.TileSelf)]
        public static void SelectTileSelf(BoardSearcherSide self, BoardSearcherSide opponent, List<BoardSearcherTile> selectedTiles) { SearchTilesFree(self, selectedTiles); }

        [TileSearcher(TileSearchType.TileOpponent)]
        public static void SelectTileOpponent(BoardSearcherSide self, BoardSearcherSide opponent, List<BoardSearcherTile> selectedTiles) { SearchTilesFree(opponent, selectedTiles); }

        [TileSearcher(TileSearchType.Unit)]
        public static void SelectUnit(BoardSearcherSide self, BoardSearcherSide opponent, List<BoardSearcherTile> selectedTiles)
        {
            SearchTilesOccupied(self, selectedTiles);
            SearchTilesOccupied(opponent, selectedTiles);
        }

        [TileSearcher(TileSearchType.UnitSelf)]
        public static void SelectUnitSelf(BoardSearcherSide self, BoardSearcherSide opponent, List<BoardSearcherTile> selectedTiles) { SearchTilesOccupied(self, selectedTiles); }

        [TileSearcher(TileSearchType.UnitOpponent)]
        public static void SelectUnitOpponent(BoardSearcherSide self, BoardSearcherSide opponent, List<BoardSearcherTile> selectedTiles) { SearchTilesOccupied(self, selectedTiles); }
    }
}
