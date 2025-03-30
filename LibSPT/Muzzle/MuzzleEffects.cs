namespace HollywoodFX.Muzzle;

internal class CurrentShot
{
    public bool Handled;
    public IWeapon Weapon = null;
    public AmmoItemClass Ammo = null;
}

public class MuzzleEffects
{
    private CurrentShot _currentShot;

    public MuzzleEffects()
    {
        _currentShot = new CurrentShot();
    }

    public void UpdateCurrentShot(IWeapon weapon, AmmoItemClass ammo)
    {
        _currentShot.Weapon = weapon;
        _currentShot.Ammo = ammo;
        _currentShot.Handled = false;
    }
}