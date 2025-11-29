namespace AutoStack_CLI;

public enum PackageManager
{
    NOTCHOSEN,
    NPM,
    PNPM,
    YARN,
    BUN
}

public class Config
{
    public PackageManager ChosenPackageManager { get; set; } = PackageManager.NOTCHOSEN;
}