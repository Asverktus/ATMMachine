using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AtmMachine
{
  public sealed class AtmController
  {
    private static readonly AtmController _instance;
    private Dictionary<string, UserAccount> _accounts;
    private UserAccount? _currentUser;
    private readonly string _dataFilePath;

    static AtmController()
    {
      _instance = new AtmController();
    }

    private AtmController()
    {
      _dataFilePath = "accounts.txt";
      _accounts = new Dictionary<string, UserAccount>();

      LoadAccountsFromFile();

      _currentUser = null;
    }

    private void LoadAccountsFromFile()
    {
      if (!File.Exists(_dataFilePath))
      {
        CreateDefaultAccounts();
        SaveAccountsToFile();
        return;
      }

      try
      {
        string[] lines = File.ReadAllLines(_dataFilePath, Encoding.UTF8);

        foreach (string line in lines)
        {
          if (string.IsNullOrWhiteSpace(line))
          {
            continue;
          }

          string[] parts = line.Split('|');

          if (parts.Length != 3)
          {
            continue;
          }

          string username = parts[0].Trim();
          string password = parts[1].Trim();

          if (!decimal.TryParse(parts[2].Trim(), out decimal balance))
          {
            continue;
          }

          _accounts.Add(username, new UserAccount(username, password, balance));
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading accounts: {ex.Message}");
        CreateDefaultAccounts();
      }
    }

    private void CreateDefaultAccounts()
    {
      _accounts.Clear();
      _accounts.Add("user1", new UserAccount("user1", "1337", 1000m));
      _accounts.Add("user2", new UserAccount("user2", "2448", 500m));
      _accounts.Add("user3", new UserAccount("user3", "3559", 9999m));
    }

    private void SaveAccountsToFile()
    {
      try
      {
        List<string> lines = new List<string>();

        foreach (UserAccount account in _accounts.Values)
        {
          string line = $"{account.Username}|{account.GetPasswordHash()}|{account.Balance}";
          lines.Add(line);
        }

        File.WriteAllLines(_dataFilePath, lines, Encoding.UTF8);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error saving accounts: {ex.Message}");
      }
    }

    public static AtmController Instance
    {
      get
      {
        return _instance;
      }
    }

    public void Run()
    {
      Console.WriteLine("=== ATM Simulator ===\n");

      bool isAuthenticated = AuthenticateUser();

      if (!isAuthenticated)
      {
        Console.WriteLine("Too many failed attempts. Exiting...");
        Console.ReadKey();
        return;
      }

      while (true)
      {
        Console.WriteLine($"\nCurrent user: {_currentUser!.Username}");
        Console.WriteLine($"Balance: {_currentUser.Balance:F2} rub\n");

        Console.WriteLine("1. Deposit");
        Console.WriteLine("2. Withdraw");
        Console.WriteLine("3. Transfer");
        Console.WriteLine("4. Check balance");
        Console.WriteLine("5. Switch user");
        Console.WriteLine("0. Exit");

        Console.Write("\nChoose action: ");
        string? input = Console.ReadLine();

        int choice;
        bool parseResult = int.TryParse(input, out choice);

        if (!parseResult)
        {
          Console.WriteLine("Invalid input. Please enter a number.");
          continue;
        }

        MenuOption selectedOption = (MenuOption)choice;

        if (selectedOption == MenuOption.Exit)
        {
          Console.WriteLine("Goodbye!");
          return;
        }

        try
        {
          switch (selectedOption)
          {
            case MenuOption.Deposit:
              PerformDeposit();
              break;

            case MenuOption.Withdraw:
              PerformWithdraw();
              break;

            case MenuOption.Transfer:
              PerformTransfer();
              break;

            case MenuOption.CheckBalance:
              CheckBalance();
              break;

            case MenuOption.SwitchUser:
              SwitchUser();
              break;

            default:
              Console.WriteLine("Invalid choice. Try again.");
              break;
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error: {ex.Message}");
        }
      }
    }

    private bool AuthenticateUser()
    {
      int maxAttempts = 3;

      for (int attempt = 1; attempt <= maxAttempts; ++attempt)
      {
        Console.Write("Enter username: ");
        string? username = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
          Console.WriteLine($"Username cannot be empty. Attempt {attempt} of {maxAttempts}");
          continue;
        }

        if (!_accounts.ContainsKey(username))
        {
          Console.WriteLine($"User '{username}' not found. Attempt {attempt} of {maxAttempts}");
          continue;
        }

        Console.Write("Enter password: ");
        string? password = ReadPassword();

        UserAccount account = _accounts[username];

        if (account.ValidatePassword(password ?? ""))
        {
          _currentUser = account;
          Console.WriteLine($"\nWelcome, {_currentUser.Username}!");
          return true;
        }

        Console.WriteLine($"Invalid password. Attempt {attempt} of {maxAttempts}");
      }

      return false;
    }

    private string ReadPassword()
    {
      string password = "";

      while (true)
      {
        ConsoleKeyInfo key;
        key = Console.ReadKey(true);

        if (key.Key == ConsoleKey.Enter)
        {
          break;
        }

        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
          password = password.Substring(0, password.Length - 1);
          Console.Write("\b \b");
        }
        else if (!char.IsControl(key.KeyChar))
        {
          password = password + key.KeyChar;
          Console.Write("*");
        }
      }

      Console.WriteLine();
      return password;
    }

    private decimal GetBalance()
    {
      if (_currentUser == null)
      {
        return 0m;
      }

      return _currentUser.Balance;
    }

    private void CheckBalance()
    {
      decimal balance = GetBalance();
      Console.WriteLine($"\nYour balance: {balance:F2} rub");
    }

    private void PerformDeposit()
    {
      if (_currentUser == null)
      {
        Console.WriteLine("No user logged in.");
        return;
      }

      decimal depositAmount = ReadPositiveDecimal("Enter amount to deposit: ");

      _currentUser.Balance = GetBalance() + depositAmount;
      SaveAccountsToFile();

      Console.WriteLine($"Deposited {depositAmount:F2} rub. New balance: {GetBalance():F2} rub");
    }

    private void PerformWithdraw()
    {
      if (_currentUser == null)
      {
        Console.WriteLine("No user logged in.");
        return;
      }

      decimal withdrawAmount = ReadPositiveDecimal("Enter amount to withdraw: ");

      decimal currentBalance = GetBalance();

      if (withdrawAmount > currentBalance)
      {
        Console.WriteLine($"Error: Insufficient funds. Available: {currentBalance:F2} rub");
        return;
      }

      _currentUser.Balance = currentBalance - withdrawAmount;
      SaveAccountsToFile();

      Console.WriteLine($"Withdrawn {withdrawAmount:F2} rub. New balance: {GetBalance():F2} rub");
    }

    private void PerformTransfer()
    {
      if (_currentUser == null)
      {
        Console.WriteLine("No user logged in.");
        return;
      }

      Console.Write("Enter target username: ");
      string? targetUsername = Console.ReadLine();

      if (string.IsNullOrWhiteSpace(targetUsername))
      {
        Console.WriteLine("Username cannot be empty.");
        return;
      }

      if (!_accounts.ContainsKey(targetUsername))
      {
        Console.WriteLine($"Error: User '{targetUsername}' not found.");
        Console.WriteLine("Available users:");

        foreach (string user in _accounts.Keys)
        {
          Console.WriteLine($"  - {user}");
        }

        return;
      }

      if (targetUsername == _currentUser.Username)
      {
        Console.WriteLine("Error: Cannot transfer to yourself.");
        return;
      }

      decimal transferAmount = ReadPositiveDecimal("Enter amount to transfer: ");

      decimal currentBalance = GetBalance();

      if (transferAmount > currentBalance)
      {
        Console.WriteLine($"Error: Insufficient funds. Available: {currentBalance:F2} rub");
        return;
      }

      _currentUser.Balance = currentBalance - transferAmount;
      _accounts[targetUsername].Balance = _accounts[targetUsername].Balance + transferAmount;
      SaveAccountsToFile();

      Console.WriteLine($"Transferred {transferAmount:F2} rub to {targetUsername}");
      Console.WriteLine($"Your new balance: {GetBalance():F2} rub");
    }

    private void SwitchUser()
    {
      _currentUser = null;

      bool isAuthenticated = AuthenticateUser();

      if (!isAuthenticated)
      {
        Console.WriteLine("Authentication failed. Returning to main menu...");
      }
    }

    private decimal ReadPositiveDecimal(string prompt)
    {
      while (true)
      {
        Console.Write(prompt);
        string? input = Console.ReadLine();

        if (!decimal.TryParse(input, out decimal amount))
        {
          Console.WriteLine("Invalid input. Please enter a valid number.");
          continue;
        }

        if (amount <= 0)
        {
          Console.WriteLine("Amount must be positive. Please try again.");
          continue;
        }

        return amount;
      }
    }
  }
}