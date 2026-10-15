public static class FarmBoxMergePlatformPolicy
{
    public static bool IsItchEdition
    {
        get
        {
#if UNITY_WEBGL || FARMBOXMERGE_ITCHIO
            return true;
#else
            return false;
#endif
        }
    }

    public static bool UsesRewardedAds => !IsItchEdition;
}
