using UnityEngine;
using System.Collections.Generic;

public class GameInitializer : MonoBehaviour
{
    [Header("Job Templates")]
    public List<Job> availableJobs = new List<Job>();
    
    [Header("House Templates")]
    public List<House> availableHouses = new List<House>();
    
    [Header("Action Templates")]
    public List<ActionData> availableActions = new List<ActionData>();
    
    [Header("Baby Templates")]
    public List<Baby> availableBabies = new List<Baby>();
    
    [Header("Game Settings")]
    public int startingMoney = 1000;
    public int startingBabies = 0;
    
    private static GameInitializer instance;
    public static GameInitializer Instance { get { return instance; } }
    
    public bool isInitialized = false;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Initialize game data immediately to ensure jobs are available
            InitializeGameData();
            isInitialized = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Call this when the game starts
    public void InitializeGame()
    {
        if (!isInitialized)
        {
            InitializeGameData();
            isInitialized = true;
        }
    }
    
    private void InitializeGameData()
    {
        InitializeJobs();
        InitializeHouses();
        InitializeActions();
        InitializeBabies();
    }
    
    private void InitializeJobs()
    {
        // Ensure the list is initialized
        if (availableJobs == null)
        {
            availableJobs = new List<Job>();
            Debug.Log("GameInitializer: Created new availableJobs list");
        }
        
        if (availableJobs.Count == 0)
        {
            Debug.Log("GameInitializer: Adding default jobs to list");
            
            // Add the Unemployed job first (starting job for all players)
            availableJobs.Add(new Job("Unemployed", "Looking for work", 0, 0, false));
            
            // Jobs that don't require education (before Video Game Designer)
            availableJobs.Add(new Job("Assassin", "A professional killer for hire", 80000, 2, false));
            availableJobs.Add(new Job("Youtuber", "Create content for millions of viewers", 20000, 4, false));
            availableJobs.Add(new Job("Happy Loan Collector", "Collect debts with a smile", 200000, 8, false));
            availableJobs.Add(new Job("Aaron", "Professional Aaron", 20000, 4, false));
            availableJobs.Add(new Job("Race Car Driver", "Drive fast cars for a living", 60000, 3, false));
            availableJobs.Add(new Job("Lifeguard", "Save lives at the beach", 320000, 0, false));
            availableJobs.Add(new Job("Draymond Green", "Professional basketball player", 30000, 3, false));
            availableJobs.Add(new Job("Babysitter", "Take care of children", 30000, 8, false));
            availableJobs.Add(new Job("Dog Walker", "Walk dogs for a living", 40000, 9, false));
            availableJobs.Add(new Job("Bus Driver", "Drive the city bus", 10000, 1, false));
            availableJobs.Add(new Job("Sabrina Carpenter", "Famous singer and actress", 660000, 5, false));
            availableJobs.Add(new Job("Pirate", "Sail the seven seas", 30000, 7, false));
            availableJobs.Add(new Job("Police Officer", "Protect and serve", 50000, 1, false));
            availableJobs.Add(new Job("Firefighter", "Save lives from fires", 50000, 2, false));
            availableJobs.Add(new Job("Thief", "Steal for a living", 10000, 4, false));
            availableJobs.Add(new Job("Janitor", "Keep things clean", 30000, 6, false));
            availableJobs.Add(new Job("Singer", "Perform music for audiences", 70000, 4, false));
            availableJobs.Add(new Job("Bee Keeper", "Take care of bees", 0, 3, false));
            availableJobs.Add(new Job("Zoo Keeper", "Take care of animals", 90000, 3, false));
            availableJobs.Add(new Job("Video Game Designer", "Create amazing games", 90000, 4, true));
            availableJobs.Add(new Job("Vet", "Help sick animals", 110000, 3, true));
            availableJobs.Add(new Job("Doctor", "Save human lives", 150000, 2, true));
            availableJobs.Add(new Job("Scientist", "Discover new things", 120000, 3, true));
            availableJobs.Add(new Job("Fashion Designer", "Create beautiful clothes", 100000, 5, true));
            availableJobs.Add(new Job("Secret Agent", "Work undercover", 200000, 1, true));
            availableJobs.Add(new Job("Lawyer", "Fight for justice in court", 130000, 4, true));
            availableJobs.Add(new Job("Teacher", "Educate the next generation", 70000, 6, true));
            
            Debug.Log($"GameInitializer: Added {availableJobs.Count} jobs to list");
        }
        else
        {
            Debug.Log($"GameInitializer: Jobs list already has {availableJobs.Count} jobs");
        }
    }
    
    private void InitializeHouses()
    {
        if (availableHouses.Count == 0)
        {
            // Add houses with their costs (base cost, red cost, black cost)
            availableHouses.Add(new House("Ranch", "A spacious ranch with plenty of land", 600000, 400000, 800000));
            availableHouses.Add(new House("Beach Hut", "A cozy hut right on the beach", 100000, 20000, 200000));
            availableHouses.Add(new House("Dream Villa", "Your dream villa with all amenities", 300000, 150000, 550000));
            availableHouses.Add(new House("Studio Apartment", "A compact studio in the city", 100000, 80000, 120000));
            availableHouses.Add(new House("Luxury Apartment", "A luxurious apartment with city views", 250000, 150000, 300000));
            availableHouses.Add(new House("Family House", "Perfect home for a growing family", 250000, 200000, 320000));
            availableHouses.Add(new House("Cozy Cottage", "A charming cottage in the countryside", 150000, 150000, 150000));
            availableHouses.Add(new House("Houseboat", "Live on the water in this unique houseboat", 200000, 100000, 300000));
            availableHouses.Add(new House("Farmhouse", "A traditional farmhouse with character", 300000, 100000, 350000));
            availableHouses.Add(new House("Eco House", "An environmentally friendly home", 200000, 0, 400000));
            availableHouses.Add(new House("Teepee", "A traditional teepee for the adventurous", 100000, 50000, 200000));
            availableHouses.Add(new House("Island Holiday Home", "Your private island getaway", 600000, 0, 1000000));
            availableHouses.Add(new House("City Penthouse", "A luxurious penthouse in the city", 600000, 500000, 700000));
            availableHouses.Add(new House("Windmill", "A converted windmill with unique charm", 350000, 150000, 500000));
        }
    }
    
    private void InitializeActions()
    {
        if (availableActions.Count == 0)
        {
            // Money actions
            availableActions.Add(new ActionData("Found Money", "You found $100 on the street!", 100));
            availableActions.Add(new ActionData("Lost Money", "You dropped $50 somewhere!", -50));
            availableActions.Add(new ActionData("Lottery Win", "You won the lottery! $500!", 500));
            availableActions.Add(new ActionData("Tax Bill", "You owe $200 in taxes!", -200));
            
            // Baby actions
            availableActions.Add(new ActionData("New Baby", "Congratulations! You have a new baby!", 0, 1));
            availableActions.Add(new ActionData("Twins!", "Surprise! You have twins!", 0, 2));
            availableActions.Add(new ActionData("Baby Adoption", "You adopted a baby!", -1000, 1));
            
            // Job actions
            availableActions.Add(new ActionData("Got Fired", "You lost your job!", -500));
            availableActions.Add(new ActionData("Promotion", "You got promoted!", 300));
            
            // Life events
            availableActions.Add(new ActionData("Got Married", "Congratulations on your wedding!", 1000));
            availableActions.Add(new ActionData("Divorce", "Your marriage ended in divorce.", -2000));
            availableActions.Add(new ActionData("Car Accident", "You had a car accident!", -1500));
            availableActions.Add(new ActionData("Medical Emergency", "You had a medical emergency!", -3000));
            availableActions.Add(new ActionData("Inheritance", "You inherited money from a relative!", 5000));
            availableActions.Add(new ActionData("Stock Market Win", "Your investments paid off!", 2000));
            availableActions.Add(new ActionData("Stock Market Loss", "Your investments lost value!", -1500));
        }
    }
    
    private void InitializeBabies()
    {
        if (availableBabies.Count == 0)
        {
            // Add default baby templates
            availableBabies.Add(new Baby("Alex", "A cheerful baby boy who loves to laugh", true, 0));
            availableBabies.Add(new Baby("Ben", "A quiet baby boy who loves to sleep", true, 0));
            availableBabies.Add(new Baby("Charlie", "An energetic baby boy who's always moving", true, 0));
            availableBabies.Add(new Baby("David", "A curious baby boy who explores everything", true, 0));
            availableBabies.Add(new Baby("Ethan", "A gentle baby boy who loves cuddles", true, 0));
            availableBabies.Add(new Baby("Frank", "A strong baby boy who's already trying to walk", true, 0));
            availableBabies.Add(new Baby("George", "A smart baby boy who watches everything", true, 0));
            availableBabies.Add(new Baby("Henry", "A playful baby boy who loves toys", true, 0));
            availableBabies.Add(new Baby("Ian", "A calm baby boy who rarely cries", true, 0));
            availableBabies.Add(new Baby("Jack", "A brave baby boy who's not afraid of anything", true, 0));
            
            availableBabies.Add(new Baby("Alice", "A sweet baby girl who loves to smile", false, 0));
            availableBabies.Add(new Baby("Beth", "A gentle baby girl who loves music", false, 0));
            availableBabies.Add(new Baby("Clara", "A bright baby girl who's very alert", false, 0));
            availableBabies.Add(new Baby("Diana", "A graceful baby girl who moves beautifully", false, 0));
            availableBabies.Add(new Baby("Emma", "A happy baby girl who spreads joy", false, 0));
            availableBabies.Add(new Baby("Fiona", "A clever baby girl who learns quickly", false, 0));
            availableBabies.Add(new Baby("Grace", "A peaceful baby girl who's very content", false, 0));
            availableBabies.Add(new Baby("Hannah", "A loving baby girl who gives hugs", false, 0));
            availableBabies.Add(new Baby("Iris", "A creative baby girl who loves colors", false, 0));
            availableBabies.Add(new Baby("Jane", "A determined baby girl who never gives up", false, 0));
        }
    }
    
    // Get a random job
    public Job GetRandomJob()
    {
        if (availableJobs.Count == 0) return null;
        return availableJobs[Random.Range(0, availableJobs.Count)];
    }
    
    // Get a random house
    public House GetRandomHouse()
    {
        if (availableHouses.Count == 0) return null;
        return availableHouses[Random.Range(0, availableHouses.Count)];
    }
    
    // Get a random action
    public ActionData GetRandomAction()
    {
        if (availableActions.Count == 0) return null;
        return availableActions[Random.Range(0, availableActions.Count)];
    }
    
    // Get a random baby
    public Baby GetRandomBaby()
    {
        if (availableBabies.Count == 0) return null;
        return availableBabies[Random.Range(0, availableBabies.Count)];
    }
    
    // Get a random baby by gender
    public Baby GetRandomBabyByGender(bool isMale)
    {
        List<Baby> genderBabies = new List<Baby>();
        foreach (Baby baby in availableBabies)
        {
            if (baby.isMale == isMale)
            {
                genderBabies.Add(baby);
            }
        }
        
        if (genderBabies.Count == 0) return null;
        return genderBabies[Random.Range(0, genderBabies.Count)];
    }
    
    // Get the game end action
    public ActionData GetGameEndAction(int winnerPlayerNumber)
    {
        if (winnerPlayerNumber == 0)
        {
            return new ActionData("Game Over - It's a Tie!", "Both players have the same amount of money when someone reached the end! What a close game!", 0);
        }
        else if (winnerPlayerNumber == GetMyPlayerNumber())
        {
            return new ActionData("You Won!", "Congratulations! You had the most money when someone reached the end of the path!", 1000);
        }
        else
        {
            return new ActionData("Game Over - You Lost", $"Player {winnerPlayerNumber} had more money when someone reached the end of the path! Better luck next time!", 0);
        }
    }
    
    // Helper method to get the current player number
    private int GetMyPlayerNumber()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null) return 0;
        return Unity.Netcode.NetworkManager.Singleton.IsHost ? 1 : 2;
    }
    
    // Get a job by title
    public Job GetJobByTitle(string title)
    {
        Debug.Log($"GameInitializer: GetJobByTitle called for '{title}'");
        
        // Ensure jobs are initialized
        if (availableJobs == null || availableJobs.Count == 0)
        {
            Debug.LogWarning($"GameInitializer: Jobs not initialized when GetJobByTitle('{title}') was called. Initializing now...");
            InitializeJobs();
        }
        
        // Double-check that the list is valid after initialization
        if (availableJobs == null)
        {
            Debug.LogError("GameInitializer: availableJobs is still null after InitializeJobs()");
            return null;
        }
        
        Debug.Log($"GameInitializer: Searching through {availableJobs.Count} jobs for '{title}'");
        
        foreach (Job job in availableJobs)
        {
            if (job != null && job.title == title)
            {
                Debug.Log($"GameInitializer: Found job '{title}'");
                return job;
            }
        }
        
        Debug.LogWarning($"GameInitializer: Job '{title}' not found in {availableJobs.Count} available jobs");
        return null;
    }
    
    // Get a house by name
    public House GetHouseByName(string name)
    {
        // Ensure houses are initialized
        if (availableHouses == null || availableHouses.Count == 0)
        {
            Debug.LogWarning($"GameInitializer: Houses not initialized when GetHouseByName('{name}') was called. Initializing now...");
            InitializeHouses();
        }
        
        foreach (House house in availableHouses)
        {
            if (house.title == name)
                return house;
        }
        return null;
    }
    
    // Initialize a player's starting inventory
    public void InitializePlayerInventory(PlayerInventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogError("GameInitializer: PlayerInventory is null");
            return;
        }
        
        if (!inventory.IsOwner)
        {
            Debug.LogWarning($"GameInitializer: Not owner of inventory (IsOwner: {inventory.IsOwner})");
            return;
        }
        
        try
        {
            Debug.Log($"GameInitializer: Starting inventory initialization for player (IsOwner: {inventory.IsOwner})");
            
            // Ensure game data is initialized
            if (!isInitialized)
            {
                Debug.Log("GameInitializer: Initializing game data...");
                InitializeGameData();
                isInitialized = true;
            }
            
            // Set starting values
            Debug.Log($"GameInitializer: Setting starting money: {startingMoney}");
            inventory.money.Value = startingMoney;
            
            Debug.Log($"GameInitializer: Setting starting babies: {startingBabies}");
            // Clear existing babies and add starting babies
            inventory.ClearBabies();
            inventory.AddBabies(startingBabies);
            
            // Set starting job (unemployed)
            Debug.Log("GameInitializer: Getting unemployed job...");
            Job unemployedJob = GetJobByTitle("Unemployed");
            if (unemployedJob != null)
            {
                Debug.Log($"GameInitializer: Setting job to: {unemployedJob.title}");
                inventory.SetJob(unemployedJob);
            }
            else
            {
                Debug.LogWarning("GameInitializer: Could not find 'Unemployed' job, creating default job");
                // Create a default unemployed job if not found
                Job defaultUnemployedJob = new Job("Unemployed", "Looking for work", 0);
                inventory.SetJob(defaultUnemployedJob);
            }
            
            // Reset all status flags
            Debug.Log("GameInitializer: Setting status flags...");
            inventory.SetMarried(false);
            inventory.SetInJail(false);
            inventory.SetDead(false);
            inventory.SetSnake(false);
            inventory.SetCarInsurance(false);
            inventory.SetSpeed(1);
            
            Debug.Log("GameInitializer: Player inventory initialized successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameInitializer: Error initializing player inventory: {e.Message}");
            Debug.LogError($"GameInitializer: Stack trace: {e.StackTrace}");
            
            // Set basic fallback values
            if (inventory != null)
            {
                try
                {
                    inventory.money.Value = startingMoney;
                    inventory.ClearBabies();
                    Debug.Log("GameInitializer: Set fallback values successfully");
                }
                catch (System.Exception fallbackError)
                {
                    Debug.LogError($"GameInitializer: Even fallback values failed: {fallbackError.Message}");
                }
            }
        }
    }
    
    // Get all available jobs
    public List<Job> GetAllJobs()
    {
        return new List<Job>(availableJobs);
    }
    
    // Get all available houses
    public List<House> GetAllHouses()
    {
        return new List<House>(availableHouses);
    }
    
    // Get all available actions
    public List<ActionData> GetAllActions()
    {
        return new List<ActionData>(availableActions);
    }
    
    // Get jobs that don't require education
    public List<Job> GetEntryLevelJobs()
    {
        List<Job> entryLevelJobs = new List<Job>();
        foreach (Job job in availableJobs)
        {
            if (!job.requiresEducation)
            {
                entryLevelJobs.Add(job);
            }
        }
        return entryLevelJobs;
    }
    
    // Get houses within a price range
    public List<House> GetHousesInPriceRange(int minPrice, int maxPrice)
    {
        List<House> affordableHouses = new List<House>();
        foreach (House house in availableHouses)
        {
            if (house.cost >= minPrice && house.cost <= maxPrice)
            {
                affordableHouses.Add(house);
            }
        }
        return affordableHouses;
    }
    
    // Get actions by type (money, baby, etc.)
    public List<ActionData> GetActionsByType(string type)
    {
        List<ActionData> filteredActions = new List<ActionData>();
        foreach (ActionData action in availableActions)
        {
            if (type == "money" && action.moneyChange != 0)
            {
                filteredActions.Add(action);
            }
            else if (type == "baby" && action.babyChange != 0)
            {
                filteredActions.Add(action);
            }
        }
        return filteredActions;
    }
    
    // Manual initialization method (can be called from other scripts)
    [ContextMenu("Initialize Game Data")]
    public void ManualInitialize()
    {
        InitializeGameData();
        isInitialized = true;
        Debug.Log("Game data initialized manually!");
    }
} 