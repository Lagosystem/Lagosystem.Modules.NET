
using System.ComponentModel;
using Lagosystem.Modules.Helpers;

namespace Lagosystem.Modules.Validations
{
    using Modules.Extensions;
    using System.Runtime.CompilerServices;

    public static partial class Validate
    {
        
        
        #region Brazil Documents (CPF, CNPJ, PIS, CNH, others documents)

        /// <summary>
        /// Validates a Brazilian CPF (Cadastro de Pessoas Físicas) number.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="cpf">The Brazilian CPF string to validate. It can contain formatting characters.</param>
        /// <returns><see langword="true"/> if the CPF is valid; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBrCpf(string? cpf) => !cpf.IsBlank() && IsBrCpf(cpf.AsSpan());

        /// <summary>
        /// Validates a Brazilian CPF (Cadastro de Pessoas Físicas) number using a <see cref="ReadOnlySpan{T}"/>.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="cpf">A read-only span of characters representing the CPF to validate.</param>
        /// <returns><see langword="true"/> if the CPF is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsBrCpf(ReadOnlySpan<char> cpf)
        {
            if (cpf.IsEmpty) return false;
            Span<char> buffer = stackalloc char[11];

            int len = FormatAndValidationHelper.ExtractOnlyNumbers(cpf, buffer);
            if (len is 0 or > 11) return false;

            FormatAndValidationHelper.FillStringZeroLeft(buffer, ref len, 11);
            if (FormatAndValidationHelper.IsRepeatedString(buffer)) return false;

            int sum1 = 0, sum2 = 0;
            for (int i = 0; i < 9; i++)
            {
                int digit = buffer[i] - '0';
                sum1 += digit * (10 - i);
                sum2 += digit * (11 - i);
            }

            int digit1 = (sum1 * 10) % 11;
            if (digit1 == 10) digit1 = 0;
            if (digit1 != buffer[9] - '0') return false;

            sum2 += digit1 * 2;
            int digit2 = (sum2 * 10) % 11;
            if (digit2 == 10) digit2 = 0;

            return digit2 == buffer[10] - '0';
        }

        /// <summary>
        /// Validates a Brazilian CNPJ (Cadastro Nacional da Pessoa Jurídica) number.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="cnpj">The Brazilian CNPJ string to validate. It can contain formatting characters.</param>
        /// <returns><see langword="true"/> if the CNPJ is valid; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBrCnpj(string? cnpj) => !cnpj.IsBlank() && IsBrCnpj(cnpj.AsSpan());

        /// <summary>
        /// Validates a Brazilian CNPJ (Cadastro Nacional da Pessoa Jurídica) number using a <see cref="ReadOnlySpan{T}"/>.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="cnpj">A read-only span of characters representing the CNPJ to validate.</param>
        /// <returns><see langword="true"/> if the CNPJ is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsBrCnpj(ReadOnlySpan<char> cnpj)
        {
            if (cnpj.IsEmpty) return false;

            Span<char> buffer = stackalloc char[14];
            int len = 0;

            foreach (char ch in cnpj)
            {
                if (ch is '.' or '/' or '-' or ' ') continue;

                char normalized = ch is >= 'a' and <= 'z' ? (char)(ch - ('a' - 'A')) : ch;
                bool isDigit = normalized is >= '0' and <= '9';
                bool isLetter = normalized is >= 'A' and <= 'Z';

                if (!isDigit && !isLetter) return false;
                if (len == 14) return false;

                buffer[len++] = normalized;
            }

            if (len == 0) return false;

            FormatAndValidationHelper.FillStringZeroLeft(buffer, ref len, 14);

            if (len != 14) return false;
            if (buffer[12] is < '0' or > '9' || buffer[13] is < '0' or > '9') return false;
            if (FormatAndValidationHelper.IsRepeatedString(buffer)) return false;

            ReadOnlySpan<int> weights1 = stackalloc int[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            ReadOnlySpan<int> weights2 = stackalloc int[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            int sum1 = 0;
            int sum2 = 0;

            for (int i = 0; i < 12; i++)
            {
                int charValue = buffer[i] - '0';
                sum1 += charValue * weights1[i];
                sum2 += charValue * weights2[i];
            }

            int remainder1 = sum1 % 11;
            int digit1 = remainder1 < 2 ? 0 : 11 - remainder1;

            if (digit1 != buffer[12] - '0') return false;

            sum2 += digit1 * weights2[12];

            int remainder2 = sum2 % 11;
            int digit2 = remainder2 < 2 ? 0 : 11 - remainder2;
            return digit2 == buffer[13] - '0';
        }

        /// <summary>
        /// Validates a Brazilian PIS/PASEP/NIT (Programa de Integração Social) number.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="pis">The Brazilian PIS/PASEP string to validate. It can contain formatting characters.</param>
        /// <returns><see langword="true"/> if the PIS/PASEP is valid; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBrPis(string? pis) => !pis.IsBlank() && IsBrPis(pis.AsSpan());

        /// <summary>
        /// Validates a Brazilian PIS/PASEP/NIT (Programa de Integração Social) number using a <see cref="ReadOnlySpan{T}"/>.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="pis">A read-only span of characters representing the PIS/PASEP to validate.</param>
        /// <returns><see langword="true"/> if the PIS/PASEP is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsBrPis(ReadOnlySpan<char> pis)
        {
            Span<char> buffer = stackalloc char[11];
            int len = FormatAndValidationHelper.ExtractOnlyNumbers(pis, buffer);

            if (len != 11 || FormatAndValidationHelper.IsRepeatedString(buffer[..len])) return false;

            ReadOnlySpan<int> weights = stackalloc int[] { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int sum = 0;

            for (int i = 0; i < 10; i++) sum += (buffer[i] - '0') * weights[i];

            sum %= 11;
            sum = sum > 0 ? 11 - sum : 0;
            while (sum > 9) sum -= 10;

            return sum == (buffer[10] - '0');
        }

        /// <summary>
        /// Validates a Brazilian Driver's License (Carteira Nacional de Habilitação - CNH) number.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="cnh">The Brazilian CNH string to validate. It can contain formatting characters.</param>
        /// <returns><see langword="true"/> if the CNH is valid; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBrDriverLicense(string? cnh) => !cnh.IsBlank() && IsBrDriverLicense(cnh.AsSpan());

        /// <summary>
        /// Validates a Brazilian Driver's license (Carteira Nacional de Habilitação - CNH) number using a <see cref="ReadOnlySpan{T}"/>.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="cnh">A read-only span of characters representing the CNH to validate.</param>
        /// <returns><see langword="true"/> if the CNH is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsBrDriverLicense(ReadOnlySpan<char> cnh)
        {
            if (cnh.IsEmpty) return false;

            Span<char> buffer = stackalloc char[11];
            int len = FormatAndValidationHelper.ExtractOnlyNumbers(cnh, buffer);

            if (len is 0 or > 11) return false;

            FormatAndValidationHelper.FillStringZeroLeft(buffer, ref len, 11);
            if (FormatAndValidationHelper.IsRepeatedString(buffer)) return false;

            int sum0 = 0;
            int sum1 = 0;
            int weightDesc = 9;
            int weightAsc = 1;

            for (int charIndexBuffer = 0; charIndexBuffer < 9; charIndexBuffer++)
            {
                int digit = buffer[charIndexBuffer] - '0';
                sum0 += digit * weightDesc--;
                sum1 += digit * weightAsc++;
            }

            int remainder1 = sum0 % 11;
            int remainder2 = sum1 % 11;

            int verifier0 = remainder1 <= 9 ? remainder1 : 0;
            int verifier1 = remainder2;

            if (remainder1 > 9) verifier1 = verifier1 - 2 < 0 ? verifier1 + 9 : verifier1 - 2;

            verifier1 = verifier1 <= 9 ? verifier1 : 0;
            return verifier0 == (buffer[9] - '0') && verifier1 == (buffer[10] - '0');
        }

        /// <summary>
        /// Validates a SUFRAMA (Superintendência da Zona Franca de Manaus) registration number.
        /// It ignores formatting characters and expects exactly 9 numeric digits validated via Module 11.
        /// </summary>
        /// <param name="suframaInsc">A <see cref="string"/> representing the SUFRAMA registration to validate.</param>
        /// <returns><see langword="true"/> if the SUFRAMA registration is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsBrSuframa(string? suframaInsc) => !suframaInsc.IsBlank() && IsBrSuframa(suframaInsc.AsSpan());

        /// <summary>
        /// Validates a SUFRAMA (Superintendência da Zona Franca de Manaus) registration number.
        /// It ignores formatting characters and expects exactly 9 numeric digits validated via Module 11.
        /// </summary>
        /// <param name="suframaInsc">A read-only span of characters representing the SUFRAMA registration to validate.</param>
        /// <returns><see langword="true"/> if the SUFRAMA registration is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsBrSuframa(ReadOnlySpan<char> suframaInsc)
        {
            if (suframaInsc.IsEmpty) return false;

            int sum = 0;
            int weight = 9;
            int count = 0;
            int actualDigit = -1;

            // Itera pela span uma única vez (O(N)), ignorando pontuações automaticamente.
            for (int i = 0; i < suframaInsc.Length; i++)
            {
                // Mágica de alta performance: se o char for um ponto (.), hífen (-) ou letra, 
                // a subtração por '0' resultará num número negativo. Como convertemos para uint,
                // ocorre o "underflow" e ele vira um número gigantesco (> 9), falhando no if abaixo
                // com custo zero (sem chamar char.IsDigit).
                uint c = (uint)(suframaInsc[i] - '0');

                if (c > 9) continue;
                count++;
                switch (count)
                {
                    case < 9:
                        sum += (int)c * weight;
                        weight--;
                        break;
                    case 9:
                        actualDigit = (int)c;
                        break;
                    default:
                        return false; // Mais de 9 dígitos numéricos torna a inscrição automaticamente inválida.
                }
            }

            if (count != 9) return false; // Se após filtrar tudo não tivermos exatamente 9 números, é inválido.

            // Cálculo do dígito pelo Módulo 11
            int remainder = sum % 11;
            int expectedDigit = remainder < 2 ? 0 : 11 - remainder;

            return actualDigit == expectedDigit;
        }

        /// <summary>
        /// Validates a Brazilian Voter Registration Card (Título de Eleitor).
        /// It ignores formatting characters and expects exactly 12 numeric digits.
        /// Uses the Module 11 validation rules specified by the TSE (Tribunal Superior Eleitoral).
        /// </summary>
        /// <param name="tituloEleitor">A string representing the Voter Registration Card to validate.</param>
        /// <returns><see langword="true"/> if the registration number is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsBrTituloEleitor(string? tituloEleitor) =>
            !tituloEleitor.IsBlank() && IsBrTituloEleitor(tituloEleitor.AsSpan());

        /// <summary>
        /// Validates a Brazilian Voter Registration Card (Título de Eleitor).
        /// It ignores formatting characters and expects exactly 12 numeric digits.
        /// Uses the Module 11 validation rules specified by the TSE (Tribunal Superior Eleitoral).
        /// </summary>
        /// <param name="tituloEleitor">A read-only span of characters representing the Voter Registration Card to validate.</param>
        /// <returns><see langword="true"/> if the registration number is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsBrTituloEleitor(ReadOnlySpan<char> tituloEleitor)
        {
            if (tituloEleitor.IsEmpty) return false;

            // 48 bytes na stack. Zero impacto no Garbage Collector.
            Span<int> digits = stackalloc int[12];
            int count = 0;
            bool allIdentical = true;

            // Loop de varredura única (O(N)) para ignorar formatação e extrair números
            for (int i = 0; i < tituloEleitor.Length; i++)
            {
                // Mesma mágica de Branchless Cast usada no SUFRAMA
                uint c = (uint)(tituloEleitor[i] - '0');
                if (c > 9) continue;
                if (count == 12) return false; // Mais de 12 números invalida o documento
                digits[count] = (int)c;

                // Checa se todos os números são iguais (ex: "000000000000")
                if (count > 0 && digits[count] != digits[count - 1])
                    allIdentical = false;

                count++;
            }

            // Título de Eleitor precisa ter exatos 12 dígitos e não pode ser uma sequência repetida
            if (count != 12 || allIdentical) return false;

            // Os dígitos 9 e 10 (índices 8 e 9) representam o código da UF (Estado)
            int uf = digits[8] * 10 + digits[9];

            // Códigos válidos de UF vão de 01 a 28 (28 representa eleitores no Exterior)
            if (uf is < 1 or > 28) return false;

            // --- Cálculo do Primeiro Dígito Verificador (DV1) ---
            // Pesos aplicados: 2, 3, 4, 5, 6, 7, 8, 9
            int sum1 = digits[0] * 2 + digits[1] * 3 + digits[2] * 4 + digits[3] * 5 +
                       digits[4] * 6 + digits[5] * 7 + digits[6] * 8 + digits[7] * 9;

            int rem1 = sum1 % 11;
            rem1 = rem1 switch
            {
                10 => 0,
                0 when uf is 1 or 2 => 1,
                var _ => rem1
            };

            if (digits[10] != rem1) return false;

            // --- Cálculo do Segundo Dígito Verificador (DV2) ---
            // Pesos aplicados: 7, 8, 9 (O cálculo usa os dois dígitos da UF e o DV1)
            int sum2 = digits[8] * 7 + digits[9] * 8 + digits[10] * 9;

            int rem2 = sum2 % 11;
            rem2 = rem2 switch
            {
                10 => 0,
                0 when uf is 1 or 2 => 1,
                var _ => rem2
            };

            return digits[11] == rem2;
        }

        /// <summary>Checks state-registration check digits without allocating a new string.</summary>
        /// <param name="ie">Registration number; ordinary punctuation is accepted.</param>
        /// <param name="state">Issuing Brazilian state, using the existing IBGE-based enum.</param>
        /// <returns>True if the supplied format and state-specific check digits are consistent.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The state is not one of the 27 Brazilian UFs.</exception>
        public static bool IsBrStateRegistration(ReadOnlySpan<char> ie, BrazilianStates state)
        {
            if (!BrazilianStateRegistrationCore.IsSupportedState(state))
                ThrowHelper.ThrowErrorStateRegistration(nameof(state));
            
            Span<char> digits = stackalloc char[14];
            return BrazilianStateRegistrationCore.TryNormalize(ie, digits, out int length, out bool rural) 
                && BrazilianStateRegistrationCore.IsValid(digits[..length], state, rural);
        }

        /// <summary>String overload for state-registration validation.</summary>
        /// <param name="ie">Registration text, potentially formatted.</param>
        /// <param name="state">Issuing Brazilian state.</param>
        /// <returns>True if the mathematical validation succeeds.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Unknown or unsupported state.</exception>
        public static bool IsBrStateRegistration(string ie, BrazilianStates state) => !ie.IsBlank() && IsBrStateRegistration(ie.AsSpan(), state);
        
        #endregion
        
        
        private static class BrazilianStateRegistrationCore
        {
            internal static bool IsSupportedState(BrazilianStates state) => state switch
            {
                BrazilianStates.AC or BrazilianStates.AL or BrazilianStates.AP or BrazilianStates.AM
                    or BrazilianStates.BA or BrazilianStates.CE or BrazilianStates.DF or BrazilianStates.ES
                    or BrazilianStates.GO or BrazilianStates.MA or BrazilianStates.MT or BrazilianStates.MS
                    or BrazilianStates.MG or BrazilianStates.PA or BrazilianStates.PB or BrazilianStates.PR
                    or BrazilianStates.PE or BrazilianStates.PI or BrazilianStates.RJ or BrazilianStates.RN
                    or BrazilianStates.RS or BrazilianStates.RO or BrazilianStates.RR or BrazilianStates.SC
                    or BrazilianStates.SP or BrazilianStates.SE or BrazilianStates.TO => true,
                var _ => false
            };

            // Only strip recognized formatting; never silently delete unrelated letters.
            internal static bool TryNormalize(ReadOnlySpan<char> expression, Span<char> destination, out int length, out bool rural)
            {
                length = 0;
                rural = false;

                if (expression.IsEmpty || expression.Length > 64)
                    return false;

                for (int index = 0; index < expression.Length; index++)
                {
                    char current = expression[index];

                    switch (current)
                    {
                        case >= '0' and <= '9' when length == destination.Length:
                            return false;
                        case >= '0' and <= '9':
                            destination[length++] = current;
                            break;
                        case 'P' or 'p' when !rural && length == 0:
                            rural = true;
                            break;
                        default:
                        {
                            if (current is '.' or '-' or '/' || char.IsWhiteSpace(current))
                                continue;
                            else
                                return false;
                        }
                    }
                }

                if (length == 0)
                    return false;

                // A repeated number has no useful identifying content.
                bool repeated = true;
                for (int index = 1; index < length; index++)
                {
                    if (destination[index] == destination[0]) continue;
                    repeated = false;
                    break;
                }

                return !repeated;
            }

            internal static bool IsValid(ReadOnlySpan<char> number, BrazilianStates state, bool rural)
            {
                if (rural)
                    return state == BrazilianStates.SP && IsSpRural(number);

                return state switch
                {
                    BrazilianStates.AC => IsAc(number),
                    BrazilianStates.AL => IsAl(number),
                    BrazilianStates.AP => IsAp(number),
                    BrazilianStates.AM => IsAm(number),
                    BrazilianStates.BA => IsBa(number),
                    BrazilianStates.CE => IsCe(number),
                    BrazilianStates.DF => IsDf(number),
                    BrazilianStates.ES => IsEs(number),
                    BrazilianStates.GO => IsGo(number),
                    BrazilianStates.MA => IsMa(number),
                    BrazilianStates.MT => IsMt(number),
                    BrazilianStates.MS => IsMs(number),
                    BrazilianStates.MG => IsMg(number),
                    BrazilianStates.PA => IsPa(number),
                    BrazilianStates.PB => IsPb(number),
                    BrazilianStates.PR => IsPr(number),
                    BrazilianStates.PE => IsPe(number),
                    BrazilianStates.PI => IsPi(number),
                    BrazilianStates.RJ => IsRj(number),
                    BrazilianStates.RN => IsRn(number),
                    BrazilianStates.RS => IsRs(number),
                    BrazilianStates.RO => IsRo(number),
                    BrazilianStates.RR => IsRr(number),
                    BrazilianStates.SC => IsSc(number),
                    BrazilianStates.SP => IsSp(number),
                    BrazilianStates.SE => IsSe(number),
                    BrazilianStates.TO => IsTo(number),
                    var _ => false
                };
            }

            private static int Digit(ReadOnlySpan<char> value, int index) => value[index] - '0';

            private static int Mod11(int sum)
            {
                int remainder = sum % 11;
                return remainder < 2 ? 0 : 11 - remainder;
            }

            private static int Mod11ComplementWithLastDigit(int sum) => (11 - sum % 11) % 10;

            private static int ModulusComplement(int sum, int modulus)
            {
                int remainder = sum % modulus;
                return remainder == 0 || (modulus == 11 && remainder == 1) ? 0 : modulus - remainder;
            }

            private static int DescendingSum(ReadOnlySpan<char> digits, int length, int firstWeight)
            {
                int sum = 0;
                for (int index = 0; index < length; index++)
                    sum += Digit(digits, index) * (firstWeight - index);
                return sum;
            }

            private static int AscendingRightSum(ReadOnlySpan<char> digits, int length, int lastWeight = 9, int resetWeight = 2)
            {
                int sum = 0;
                int weight = 2;
                for (int index = length - 1; index >= 0; index--)
                {
                    sum += Digit(digits, index) * weight;
                    weight = weight == lastWeight ? resetWeight : weight + 1;
                }

                return sum;
            }

            private static bool IsSimpleNine(ReadOnlySpan<char> digits) => digits.Length == 9 
                && Mod11(DescendingSum(digits, 8, 9)) == Digit(digits, 8);

            private static bool IsAc(ReadOnlySpan<char> d) =>
                d.Length == 13 && d[0] == '0' && d[1] == '1' &&
                Mod11(AscendingRightSum(d, 11)) == Digit(d, 11) &&
                Mod11(AscendingRightSum(d, 12)) == Digit(d, 12);

            private static bool IsAl(ReadOnlySpan<char> d)
            {
                if (d.Length != 9 || d[0] != '2' || d[1] != '4')
                    return false;

                char kind = d[2];
                if (kind != '0' && kind != '3' && kind != '5' && kind != '7' && kind != '8')
                    return false;

                return (DescendingSum(d, 8, 9) * 10 % 11) % 10 == Digit(d, 8);
            }

            private static bool IsAm(ReadOnlySpan<char> d) => IsSimpleNine(d);

            private static bool IsAp(ReadOnlySpan<char> d)
            {
                if (d.Length != 9 || d[0] != '0' || d[1] != '3')
                    return false;

                int number = 0;
                for (int index = 0; index < 8; index++)
                    number = number * 10 + Digit(d, index);

                if (number < 3000001)
                    return false;

                int p = number <= 3017000 ? 5 : number <= 3019022 ? 9 : 0;
                int specialDigit = number > 3017000 && number <= 3019022 ? 1 : 0;
                int complement = 11 - (DescendingSum(d, 8, 9) + p) % 11;
                int expected = complement == 11 ? specialDigit : complement == 10 ? 0 : complement;

                return expected == Digit(d, 8);
            }

            private static bool IsBa(ReadOnlySpan<char> d)
            {
                if (d.Length != 8 && d.Length != 9)
                    return false;

                // Historical 8-digit numbers use the first digit;
                // 9-digit numbers use the second to choose modulo 10/11.
                int selector = Digit(d, d.Length == 8 ? 0 : 1);
                int modulus = selector is 6 or 7 or 9 ? 11 : 10;
                int baseLength = d.Length - 2;
                int second = ModulusComplement(DescendingSum(d, baseLength, d.Length - 1), modulus);
                int first = ModulusComplement(DescendingSum(d, baseLength, d.Length) + second * 2, modulus);

                return first == Digit(d, baseLength) && second == Digit(d, baseLength + 1);
            }

            private static bool IsCe(ReadOnlySpan<char> d) => IsSimpleNine(d);

            private static bool IsDf(ReadOnlySpan<char> d) => d.Length == 13 
                && d[0] == '0' && (d[1] == '7' || d[1] == '8') 
                && Mod11(AscendingRightSum(d, 11)) == Digit(d, 11) 
                && Mod11(AscendingRightSum(d, 12)) == Digit(d, 12);

            private static bool IsEs(ReadOnlySpan<char> d) => IsSimpleNine(d);

            private static bool IsGo(ReadOnlySpan<char> d)
            {
                if (d.Length != 9 || !((d[0] == '1' && (d[1] == '0' || d[1] == '1' || d[1] == '5'))))
                    return false;

                int baseNumber = 0;
                for (int index = 0; index < 8; index++)
                    baseNumber = baseNumber * 10 + Digit(d, index);

                if (baseNumber == 11094402)
                    return d[8] == '0' || d[8] == '1';

                int remainder = DescendingSum(d, 8, 9) % 11;
                int expected = remainder switch
                {
                    0 => 0,
                    1 => (baseNumber is >= 10103105 and <= 10119997 ? 1 : 0),
                    var _ => 11 - remainder
                };
                return Digit(d, 8) == expected;
            }

            private static bool IsMa(ReadOnlySpan<char> d) => d.Length == 9 && d[0] == '1' && d[1] == '2' && IsSimpleNine(d);

            private static bool IsMt(ReadOnlySpan<char> d)
            {
                // MT accepts the 9-digit canonical form and the 11-digit
                // historical transport representation (two initial zeros).
                if (d.Length != 9) 
                    return d.Length == 11 && Mod11(AscendingRightSum(d, 10)) == Digit(d, 10);
                Span<char> padded = stackalloc char[11];
                padded[0] = '0';
                padded[1] = '0';
                d.CopyTo(padded[2..]);
                return Mod11(AscendingRightSum(padded, 10)) == Digit(padded, 10);

            }

            private static bool IsMs(ReadOnlySpan<char> d) => d.Length == 9 && d[0] == '2' && d[1] == '8' && IsSimpleNine(d);

            private static bool IsMg(ReadOnlySpan<char> d)
            {
                if (d.Length != 13)
                    return false;

                // The first check digit is a Mod 10 calculation with an
                // inserted 0 after the third digit and alternating weights 1,2.
                int sum = 0;
                for (int index = 0; index < 12; index++)
                {
                    int digit = index == 3 ? 0 : Digit(d, index < 3 ? index : index - 1);
                    int product = digit * (index % 2 == 0 ? 1 : 2);
                    sum += product / 10 + product % 10;
                }

                int first = (10 - sum % 10) % 10;
                return first == Digit(d, 11) && Mod11(AscendingRightSum(d, 12, 11)) == Digit(d, 12);
            }

            private static bool IsPa(ReadOnlySpan<char> d) => d.Length == 9 && d[0] == '1' && d[1] == '5' && IsSimpleNine(d);

            private static bool IsPb(ReadOnlySpan<char> d) => IsSimpleNine(d);

            private static bool IsPr(ReadOnlySpan<char> d) => d.Length == 10 &&
              Mod11(AscendingRightSum(d, 8, 7)) == Digit(d, 8) &&
              Mod11(AscendingRightSum(d, 9, 7)) == Digit(d, 9);

            private static bool IsPe(ReadOnlySpan<char> d)
            {
                if (d.Length == 9)
                    return Mod11(DescendingSum(d, 7, 8)) == Digit(d, 7) 
                           && Mod11(DescendingSum(d, 8, 9)) == Digit(d, 8);

                if (d.Length != 14)
                    return false;

                int sum = 0;
                int weight = 2;
                for (int index = 12; index >= 0; index--)
                {
                    sum += Digit(d, index) * weight;
                    weight = weight == 9 ? 1 : weight + 1;
                }

                return Mod11ComplementWithLastDigit(sum) == Digit(d, 13);
            }

            private static bool IsPi(ReadOnlySpan<char> d) => IsSimpleNine(d);

            private static bool IsRj(ReadOnlySpan<char> d) => d.Length == 8 && Mod11(AscendingRightSum(d, 7, 7)) == Digit(d, 7);

            private static bool IsRn(ReadOnlySpan<char> d)
            {
                if ((d.Length != 9 && d.Length != 10) || d[0] != '2' || d[1] != '0')
                    return false;

                int sum = 0;
                for (int index = d.Length - 2, weight = 2; index >= 0; index--, weight++)
                    sum += Digit(d, index) * weight;

                return (sum * 10 % 11) % 10 == Digit(d, d.Length - 1);
            }

            private static bool IsRo(ReadOnlySpan<char> d) => d.Length == 14 && Mod11ComplementWithLastDigit(AscendingRightSum(d, 13)) == Digit(d, 13);

            private static bool IsRr(ReadOnlySpan<char> d)
            {
                if (d.Length != 9 || d[0] != '2' || d[1] != '4')
                    return false;

                int sum = 0;
                for (int index = 0; index < 8; index++)
                    sum += Digit(d, index) * (index + 1);

                return sum % 9 == Digit(d, 8);
            }

            private static bool IsRs(ReadOnlySpan<char> d) => d.Length == 10 && Mod11(AscendingRightSum(d, 9)) == Digit(d, 9);

            private static bool IsSc(ReadOnlySpan<char> d) => d.Length == 9 && Mod11(AscendingRightSum(d, 8)) == Digit(d, 8);

            private static int SpFirstDigit(ReadOnlySpan<char> d)
            {
                ReadOnlySpan<byte> weights = stackalloc byte[] { 1, 3, 4, 5, 6, 7, 8, 10 };
                int sum = 0;
                for (int index = 0; index < 8; index++)
                    sum += Digit(d, index) * weights[index];

                return sum % 11 % 10;
            }

            private static bool IsSpRural(ReadOnlySpan<char> d) => d.Length == 12 && SpFirstDigit(d) == Digit(d, 8);

            private static bool IsSp(ReadOnlySpan<char> d)
            {
                if (d.Length != 12 || SpFirstDigit(d) != Digit(d, 8))
                    return false;

                ReadOnlySpan<byte> weights = stackalloc byte[] { 3, 2, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
                int sum = 0;
                for (int index = 0; index < 11; index++)
                    sum += Digit(d, index) * weights[index];

                return sum % 11 % 10 == Digit(d, 11);
            }

            private static bool IsSe(ReadOnlySpan<char> d) => IsSimpleNine(d);

            private static bool IsTo(ReadOnlySpan<char> d)
            {
                if (d.Length == 9)
                    return IsSimpleNine(d);

                if (d.Length != 11 || !(d[2] == '0' && (d[3] == '1' || d[3] == '2' || d[3] == '3') || d[2] == '9' && d[3] == '9'))
                    return false;

                // The two activity digits (positions 3 and 4) do not enter the DV.
                int sum = Digit(d, 0) * 9 + Digit(d, 1) * 8;
                for (int index = 4, weight = 7; index <= 9; index++, weight--)
                    sum += Digit(d, index) * weight;

                return Mod11(sum) == Digit(d, 10);
            }
        }
        
        
    }

    

}

/// <summary>
/// Brazilian federative units. Enum values are the IBGE state codes.
/// </summary>
public enum BrazilianStates
{
    /// <summary>Acre.</summary>
    AC = 12,
    /// <summary>Alagoas.</summary>
    AL = 27,
    /// <summary>Amapa.</summary>
    AP = 16,
    /// <summary>Amazonas.</summary>
    AM = 13,
    /// <summary>Bahia.</summary>
    BA = 29,
    /// <summary>Ceara.</summary>
    CE = 23,
    /// <summary>Distrito Federal.</summary>
    DF = 53,
    /// <summary>Espirito Santo.</summary>
    ES = 32,
    /// <summary>Goias.</summary>
    GO = 52,
    /// <summary>Maranhao.</summary>
    MA = 21,
    /// <summary>Mato Grosso.</summary>
    MT = 51,
    /// <summary>Mato Grosso do Sul.</summary>
    MS = 50,
    /// <summary>Minas Gerais.</summary>
    MG = 31,
    /// <summary>Para.</summary>
    PA = 15,
    /// <summary>Paraiba.</summary>
    PB = 25,
    /// <summary>Parana.</summary>
    PR = 41,
    /// <summary>Pernambuco.</summary>
    PE = 26,
    /// <summary>Piaui.</summary>
    PI = 22,
    /// <summary>Rio de Janeiro.</summary>
    RJ = 33,
    /// <summary>Rio Grande do Norte.</summary>
    RN = 24,
    /// <summary>Rio Grande do Sul.</summary>
    RS = 43,
    /// <summary>Rondonia.</summary>
    RO = 11,
    /// <summary>Roraima.</summary>
    RR = 14,
    /// <summary>Santa Catarina.</summary>
    SC = 42,
    /// <summary>Sao Paulo.</summary>
    SP = 35,
    /// <summary>Sergipe.</summary>
    SE = 28,
    /// <summary>Tocantins.</summary>
    TO = 17,
    /// <summary>Unknown / outside Brazil. Not an official IBGE code.</summary>
    ZZ = 99
}
