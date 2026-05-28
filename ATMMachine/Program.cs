using System;

namespace AtmMachine
{
  public enum MenuOption
  {
    Exit = 0,
    Deposit = 1,
    Withdraw = 2,
    Transfer = 3,
    CheckBalance = 4,
    SwitchUser = 5
  }

  public class Program
  {
    private static void Main()
    {
      AtmController controller;
      controller = AtmController.Instance;

      controller.Run();
    }
  }
}