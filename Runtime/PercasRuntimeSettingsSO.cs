using UnityEngine;

namespace Percas.Helper
{
    [CreateAssetMenu(fileName = "PercasRuntimeSettings", menuName = "Percas/Runtime Settings")]
    public class PercasRuntimeSettingsSO : ScriptableObject
    {
        public bool ShowLogs = true;
        public int FrameRate = 60;
    }
}