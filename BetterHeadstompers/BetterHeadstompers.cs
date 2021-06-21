using BepInEx;
using RoR2;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using BepInEx.Configuration;

namespace BetterHeadstompers
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class BetterHeadstompers : BaseUnityPlugin
	{
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Owl777";
        public const string PluginName = "BetterHeadstompers";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<KeyCode> keybindConfigEntry;
        private static bool fallBootsEnabled = true;

        public void Awake()
        {
            Log.Init(Logger);
            keybindConfigEntry = Config.Bind<KeyCode>(
                "Keybinding",
                "Keybinding",
                KeyCode.Alpha1,
                "Key used to toggle H3AD-5T v2 jump boost."
            );
        }

        // Fall boots lights turn off when this method is added as an event.
        private void FallBootsLights_Update(ILContext il) 
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<FallBootsLights>("sourceStateMachine"),
                x => x.MatchCallvirt<EntityStateMachine>("get_state"),
                x => x.MatchIsinst<EntityStates.Headstompers.HeadstompersCooldown>(),
                x => x.MatchLdnull(),
                x => x.MatchCgtUn()
            );
            // replace this line:
            // bool flag = this.sourceStateMachine && !(this.sourceStateMachine.state is HeadstompersCooldown);
            cursor.RemoveRange(6);
            // with this line:
            // bool flag = false;
            cursor.Emit(OpCodes.Ldc_I4_1);
            // the next line in the Update method would be the comparison with the isReady flag (flag != isReady).
            // if isReady is true, then inside the if block the visual effect is added (if flag is true) or removed (if flag is false).
        }

        // Disables jump boost.
        private void HeadstompersIdle_FixedUpdateAuthority(ILContext il)
        {
            var cursor = new ILCursor(il);
            ILLabel label = null;
            cursor.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCall<EntityStates.Headstompers.BaseHeadstompersState>("get_isGrounded"),
                x => x.MatchBrfalse(out label),
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(1),
                x => x.MatchStfld<EntityStates.Headstompers.HeadstompersIdle>("jumpBoostOk")
            );
            cursor.Index += 4;
            cursor.Remove();
            // Set jumpBoostOk to false.
            cursor.Emit(OpCodes.Ldc_I4_0);
        }

        private void Update()
        {
            if (Input.GetKeyDown(keybindConfigEntry.Value))
            {   
                fallBootsEnabled = !fallBootsEnabled;
                if (fallBootsEnabled)
                {
                    IL.RoR2.FallBootsLights.Update -= FallBootsLights_Update;
                    IL.EntityStates.Headstompers.HeadstompersIdle.FixedUpdateAuthority -= HeadstompersIdle_FixedUpdateAuthority;
                }
                else
                {
                    IL.RoR2.FallBootsLights.Update += FallBootsLights_Update;
                    IL.EntityStates.Headstompers.HeadstompersIdle.FixedUpdateAuthority += HeadstompersIdle_FixedUpdateAuthority;
                }
                Log.LogInfo($"FallBoots Enabled: {fallBootsEnabled}");
            }
        }
    }
}
