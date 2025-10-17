namespace Katana.Core.Enums;

public enum TransactionType
{
    Income,
    Expense
}

public enum AccountingCategory
{
    // Income Categories
    Sale,
    ServiceRevenue,
    Interest,
    OtherIncome,
    
    // Expense Categories
    Purchase,
    Salary,
    Rent,
    Utilities,
    Marketing,
    Transportation,
    OfficeSupplie,
    Insurance,
    Tax,
    OtherExpense
}

public enum PaymentMethod
{
    Cash,
    CreditCard,
    DebitCard,
    BankTransfer,
    Cheque,
    Other
}
