namespace AtmMachine
{
  public class UserAccount
  {
    public string Username { get; private set; }
    private string _passwordHash;
    public decimal Balance { get; set; }

    public UserAccount(string username, string password, decimal initialBalance)
    {
      Username = username;
      _passwordHash = password;
      Balance = initialBalance;
    }

    public bool ValidatePassword(string password)
    {
      return _passwordHash == password;
    }

    public string GetPasswordHash()
    {
      return _passwordHash;
    }
  }
}