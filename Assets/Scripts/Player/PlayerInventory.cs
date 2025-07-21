using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerInventory : NetworkBehaviour
{
    
    [Header("Network Variables")]
    public NetworkVariable<int> money = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkList<NetworkBabyData> babies = new NetworkList<NetworkBabyData>(new List<NetworkBabyData>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<NetworkJobData> job = new NetworkVariable<NetworkJobData>(new NetworkJobData("", "", 0), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> speed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkList<NetworkHouseData> houses = new NetworkList<NetworkHouseData>(new List<NetworkHouseData>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Bools")]
    public NetworkVariable<bool> isMale = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isMarried = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isEmployed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> hasCarInsurance = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> inJail = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isSnake = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    // Events for UI updates
    public System.Action<int> OnMoneyChanged;
    public System.Action OnBabiesChanged;
    public System.Action OnBoysChanged;
    public System.Action OnGirlsChanged;
    public System.Action OnHousesChanged;
    public System.Action OnJobChanged;
    
    public override void OnNetworkSpawn()
    {
        // Subscribe to changes for ALL clients (not just owner)
        money.OnValueChanged += OnMoneyValueChanged;
        babies.OnListChanged += OnBabiesListChanged;
        job.OnValueChanged += OnJobValueChanged;
        houses.OnListChanged += OnHousesListChanged;
        
        // Don't initialize inventory here - wait for game to start
        // The TurnManager will handle initialization when the game begins
    }
    
    public override void OnNetworkDespawn()
    {
        // Unsubscribe from changes
        money.OnValueChanged -= OnMoneyValueChanged;
        babies.OnListChanged -= OnBabiesListChanged;
        job.OnValueChanged -= OnJobValueChanged;
        houses.OnListChanged -= OnHousesListChanged;
    }
    
    private void OnMoneyValueChanged(int previousValue, int newValue)
    {
        // Notify UI or other systems about money change
        OnMoneyChanged?.Invoke(newValue);
    }
    
    private void OnBabiesListChanged(NetworkListEvent<NetworkBabyData> changeEvent)
    {
        // Notify UI or other systems about babies change
        OnBabiesChanged?.Invoke();
        OnBoysChanged?.Invoke();
        OnGirlsChanged?.Invoke();
    }
    
    private void OnJobValueChanged(NetworkJobData previousValue, NetworkJobData newValue)
    {
        // Notify UI or other systems about job change
        OnJobChanged?.Invoke();
    }
    
    private void OnHousesListChanged(NetworkListEvent<NetworkHouseData> changeEvent)
    {
        // Notify UI or other systems about houses change
        OnHousesChanged?.Invoke();
    }
    
    // Add money (only owner can call this)
    public void AddMoney(int amount)
    {
        if (!IsOwner) return;
        
        money.Value += amount;
    }
    
    // Remove money (only owner can call this)
    public bool RemoveMoney(int amount)
    {
        if (!IsOwner) return false;
        
        if (money.Value >= amount)
        {
            money.Value -= amount;
            return true;
        }
        return false;
    }
    
    // Get current money (anyone can read)
    public int GetMoney()
    {
        return money.Value;
    }
    
    // Check if player has enough money
    public bool HasEnoughMoney(int amount)
    {
        return money.Value >= amount;
    }
    
    // Baby methods
    public void AddBaby(BabyData baby)
    {
        if (!IsOwner) return;
        babies.Add(new NetworkBabyData(baby));
    }
    
    public void AddRandomBaby()
    {
        if (!IsOwner) return;
        
        bool isMale = Random.Range(0, 2) == 0;
        
        // Try to get a baby template from GameInitializer
        Baby babyTemplate = GameInitializer.Instance?.GetRandomBabyByGender(isMale);
        
        if (babyTemplate != null)
        {
            // Use the template
            BabyData newBaby = new BabyData(babyTemplate.title, babyTemplate.description, babyTemplate.isMale, babyTemplate.age);
            babies.Add(new NetworkBabyData(newBaby));
        }
        else
        {
            // Fallback to hardcoded names if no templates available
            string[] boyNames = { "Alex", "Ben", "Charlie", "David", "Ethan", "Frank", "George", "Henry", "Ian", "Jack" };
            string[] girlNames = { "Alice", "Beth", "Clara", "Diana", "Emma", "Fiona", "Grace", "Hannah", "Iris", "Jane" };
            
            string name = isMale ? boyNames[Random.Range(0, boyNames.Length)] : girlNames[Random.Range(0, girlNames.Length)];
            string description = isMale ? $"A lovely baby boy named {name}" : $"A beautiful baby girl named {name}";
            
            BabyData newBaby = new BabyData(name, description, isMale, 0);
            babies.Add(new NetworkBabyData(newBaby));
        }
    }
    
    public void AddBabies(int count)
    {
        if (!IsOwner) return;
        
        for (int i = 0; i < count; i++)
        {
            AddRandomBaby();
        }
    }
    
    public void RemoveBaby(int index)
    {
        if (!IsOwner || index < 0 || index >= babies.Count) return;
        babies.RemoveAt(index);
    }
    
    public void RemoveRandomBaby()
    {
        if (!IsOwner || babies.Count == 0) return;
        babies.RemoveAt(Random.Range(0, babies.Count));
    }
    
    public void RemoveBabies(int count)
    {
        if (!IsOwner) return;
        
        int totalBabies = babies.Count;
        if (totalBabies < count) count = totalBabies;
        
        for (int i = 0; i < count; i++)
        {
            RemoveRandomBaby();
        }
    }
    
    public int GetTotalBabies()
    {
        return babies.Count;
    }
    
    public int GetBoysCount()
    {
        int count = 0;
        foreach (NetworkBabyData baby in babies)
        {
            if (baby.isMale) count++;
        }
        return count;
    }
    
    public int GetGirlsCount()
    {
        int count = 0;
        foreach (NetworkBabyData baby in babies)
        {
            if (!baby.isMale) count++;
        }
        return count;
    }
    
    public BabyData? GetBaby(int index)
    {
        if (index < 0 || index >= babies.Count) return null;
        return babies[index].ToBabyData();
    }
    
    public List<BabyData> GetAllBabies()
    {
        List<BabyData> result = new List<BabyData>();
        foreach (NetworkBabyData networkBaby in babies)
        {
            result.Add(networkBaby.ToBabyData());
        }
        return result;
    }
    
    public void ClearBabies()
    {
        if (!IsOwner) return;
        babies.Clear();
    }
    
    // Job methods
    public void SetJob(Job newJob)
    {
        if (!IsOwner) return;
        job.Value = newJob?.ToJobData() != null ? new NetworkJobData(newJob.ToJobData()) : new NetworkJobData("", "", 0);
        isEmployed.Value = (newJob != null);
        OnJobChanged?.Invoke();
    }
    
    public JobData GetJob()
    {
        return job.Value.ToJobData();
    }
    
    // House methods
    public void AddHouse(House newHouse)
    {
        if (!IsOwner) return;
        houses.Add(new NetworkHouseData(newHouse?.ToHouseData() ?? new HouseData("", "", 0, 0, 0)));
        OnHousesChanged?.Invoke();
    }
    
    public void RemoveHouse(int index)
    {
        if (!IsOwner || index < 0 || index >= houses.Count) return;
        houses.RemoveAt(index);
        OnHousesChanged?.Invoke();
    }
    
    public HouseData? GetHouse(int index)
    {
        if (index < 0 || index >= houses.Count) return null;
        return houses[index].ToHouseData();
    }
    
    public int GetHouseCount()
    {
        return houses.Count;
    }
    
    public void ClearHouses()
    {
        if (!IsOwner) return;
        houses.Clear();
        OnHousesChanged?.Invoke();
    }
    
    // Status methods
    public void SetMarried(bool married)
    {
        if (!IsOwner) return;
        isMarried.Value = married;
    }
    
    public void SetInJail(bool jailed)
    {
        if (!IsOwner) return;
        inJail.Value = jailed;
    }
    
    public void SetDead(bool dead)
    {
        if (!IsOwner) return;
        isDead.Value = dead;
    }
    
    public void SetSnake(bool snake)
    {
        if (!IsOwner) return;
        isSnake.Value = snake;
    }
    
    public void SetCarInsurance(bool hasInsurance)
    {
        if (!IsOwner) return;
        hasCarInsurance.Value = hasInsurance;
    }
    
    public void SetSpeed(int newSpeed)
    {
        if (!IsOwner) return;
        speed.Value = newSpeed;
    }
}