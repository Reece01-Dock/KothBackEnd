namespace KothBackend.Models
{
    public class PlayerPreset
    {
        public string? m_primaryWeaponResource { get; set; }
        public string? m_secondaryWeaponResource { get; set; }
        public string? m_opticWeaponResource { get; set; }
        public string? m_muzzleResource { get; set; }
        public string? m_launcherResource { get; set; }
        public List<string> m_throwableResource { get; set; } = new();
        public string? m_rangeFinderResource { get; set; }
        public string? m_viperhoodResource { get; set; }
    }
}