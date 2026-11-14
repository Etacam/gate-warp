using MioGame;
using MioModdingApi;
using MioModLoader;
using System.Runtime.CompilerServices;

namespace GateWarp
{
    public class GateWarp : Mod
    {

        private static Dictionary<string, string> gateNames = [];
        private static Dictionary<string, Vec_float_2> gatePositions = [];

        public override void Initialize()
        {
            Hooks();

            ReloadData();

            ConnectMenus();
        }

        public static void ReloadData()
        {
            using (StreamReader file = System.IO.File.OpenText(@".\mods\GateWarp\warps.dat"))
            {
                string asText = file.ReadToEnd();
                foreach (string line in asText.Split("\n"))
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    string[] parts = line.Split(":");
                    string id = parts[0];
                    string name = parts[1];
                    gateNames[id] = name;
                    Vec_float_2 pos = VectFromString(parts[2]);
                    gatePositions[id] = pos;
                }
            }
        }


        private static ModMenuPage? modPage;
        private static ChoiceOption? warpLocation;
        private static ToggleOption? unlockNessesity;
        public void ConnectMenus()
        {
            modPage = ModMenu.RegisterMod(this, "GateWarp", "");
            modPage.RegisterButton("warp", "Warp", Warp);
            IReadOnlyList<string> locs = gateNames.Values.ToList<string>();
            warpLocation = modPage.RegisterChoice("warp_loc", "Warp Location", locs, 0, 
                "The location to warp to");
            unlockNessesity = modPage.RegisterToggle("need_unlock", "Visit Nessesity", false,
                "Wether you need to have visited the checkpoint in order to warp.");
        }

        private unsafe void Hooks()
        {
            On.MioGame.On_Game.fixed_update.Prefix += Fixed_update_Prefix;
        }


        private bool queuedWarp = false;

        private unsafe void Fixed_update_Prefix(MioGame.Game* __this)
        {
            if (!queuedWarp) return;
            try
            {
                ref var mio = ref __this->mio;
                if (unlockNessesity.Value)
                {
                    ref var save = ref __this->save;
                    MioGame.String mioString = Util.StringToMioString("CHECKPOINT:" + GetSelected());
                    if (!(Unsafe.AsRef<Hashmap_String_Save_entry.Slot>(save.entries.lookup(&mioString, false)).value.count > 0))
                    {
                        queuedWarp = false;
                        return;
                    }
                }
                if (mio.cutscene.active || mio.checkpoint.state._value != Mio.Checkpoint.State.None) return;
                mio.move_to(GetWarpPosition());
                queuedWarp = false;
            }
            catch (Exception ex)
            {
                LogMessage("Could not warp: " + ex);
            }
        }

        public void Warp()
        {
            queuedWarp = true;
        }

        private static string GetSelected()
        {
            if (warpLocation == null) return "";
            return gateNames.FirstOrDefault(x => x.Value == warpLocation.Value).Key;
        }

        private static Vec_float_3 GetWarpPosition()
        {
            if (warpLocation == null) return new Vec_float_3();
            return GetWorldPos(gatePositions[GetSelected()]);
        }


        private static Vec_float_2 VectFromString(string str)
        {
            str = str.Substring(1, str.Length - 2);
            string[] parts = str.Split(", ");
            Vec_float_2 result = new()
            {
                x = float.Parse(parts[0]),
                y = float.Parse(parts[1])
            };
            return result;
        }

        private static Vec_float_3 GetWorldPos(Vec_float_2 coords)
        {
            Vec_float_3 result = new()
            {
                x = coords.x,
                y = coords.y
            };
            return result;
        }
    }
}
