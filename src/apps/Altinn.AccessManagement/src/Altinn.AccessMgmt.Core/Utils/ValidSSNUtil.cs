namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Utility class for working with Norwegian social security numbers (SSN)
/// </summary>
public static class ValidSSNUtil
{
    /// <summary>
    /// Validates that a given social security number is valid.
    /// </summary>
    /// <param name="ssnNo">
    /// Social security number to validate
    /// </param>
    /// <returns>
    /// true if valid, false otherwise.
    /// </returns>
    /// <remarks>
    /// Validates length, numeric and modulus 11.
    /// </remarks>
    public static bool IsValidSSN(string ssnNo)
    {
        int[] weightDigit10 = { 3, 7, 6, 1, 8, 9, 4, 5, 2 };
        int[] weightDigit11 = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

        // Validation only done for 11 digit numbers
        if (ssnNo.Length == 11)
        {
            try
            {
                int currentDigit = 0;
                int sumCtrlDigit10 = 0;
                int sumCtrlDigit11 = 0;
                int ctrlDigit10 = -1;
                int ctrlDigit11 = -1;

                // Calculate control digits
                for (int i = 0; i < 9; i++)
                {
                    currentDigit = int.Parse(ssnNo.Substring(i, 1));
                    sumCtrlDigit10 += currentDigit * weightDigit10[i];
                    sumCtrlDigit11 += currentDigit * weightDigit11[i];
                }

                ctrlDigit10 = 11 - (sumCtrlDigit10 % 11);
                if (ctrlDigit10 == 11)
                {
                    ctrlDigit10 = 0;
                }

                sumCtrlDigit11 += ctrlDigit10 * weightDigit11[9];
                ctrlDigit11 = 11 - (sumCtrlDigit11 % 11);
                if (ctrlDigit11 == 11)
                {
                    ctrlDigit11 = 0;
                }

                // Validate control digits in ssn
                bool digit10Valid = ctrlDigit10 == int.Parse(ssnNo.Substring(9, 1));
                bool digit11Valid = ctrlDigit11 == int.Parse(ssnNo.Substring(10, 1));
                return digit10Valid && digit11Valid;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
