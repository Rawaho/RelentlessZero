using RelentlessZero.Entities;
using RelentlessZero.Logging;
using System.Collections.Concurrent;

namespace RelentlessZero.Managers
{
    public class BattleManager
    {
        public static ConcurrentDictionary<uint, Battle> Battles = new ConcurrentDictionary<uint, Battle>();
    }
}
