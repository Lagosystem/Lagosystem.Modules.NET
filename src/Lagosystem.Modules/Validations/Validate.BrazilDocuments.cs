
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
        public static bool IsCpf(string? cpf) => !cpf.IsBlank() && IsCpf(cpf.AsSpan());

        /// <summary>
        /// Validates a Brazilian CPF (Cadastro de Pessoas Físicas) number using a <see cref="ReadOnlySpan{T}"/>.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="cpf">A read-only span of characters representing the CPF to validate.</param>
        /// <returns><see langword="true"/> if the CPF is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsCpf(ReadOnlySpan<char> cpf)
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
        public static bool IsCnpj(string? cnpj) => !cnpj.IsBlank() && IsCnpj(cnpj.AsSpan());

        /// <summary>
        /// Validates a Brazilian CNPJ (Cadastro Nacional da Pessoa Jurídica) number using a <see cref="ReadOnlySpan{T}"/>.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="cnpj">A read-only span of characters representing the CNPJ to validate.</param>
        /// <returns><see langword="true"/> if the CNPJ is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsCnpj(ReadOnlySpan<char> cnpj)
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

            if (len != 14)  return false;
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
        public static bool IsPis(string? pis) => !pis.IsBlank() && IsPis(pis.AsSpan());

        /// <summary>
        /// Validates a Brazilian PIS/PASEP/NIT (Programa de Integração Social) number using a <see cref="ReadOnlySpan{T}"/>.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="pis">A read-only span of characters representing the PIS/PASEP to validate.</param>
        /// <returns><see langword="true"/> if the PIS/PASEP is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsPis(ReadOnlySpan<char> pis)
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
        public static bool IsDriverLicense(string? cnh) => !cnh.IsBlank() && IsDriverLicense(cnh.AsSpan());

        /// <summary>
        /// Validates a Brazilian Driver's license (Carteira Nacional de Habilitação - CNH) number using a <see cref="ReadOnlySpan{T}"/>.
        /// Performs the validation without allocating extra memory (Zero Alloc).
        /// </summary>
        /// <param name="cnh">A read-only span of characters representing the CNH to validate.</param>
        /// <returns><see langword="true"/> if the CNH is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsDriverLicense(ReadOnlySpan<char> cnh)
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
        public static bool IsSuframa(string? suframaInsc) => !suframaInsc.IsBlank() && IsSuframa(suframaInsc.AsSpan());
        
        /// <summary>
        /// Validates a SUFRAMA (Superintendência da Zona Franca de Manaus) registration number.
        /// It ignores formatting characters and expects exactly 9 numeric digits validated via Module 11.
        /// </summary>
        /// <param name="suframaInsc">A read-only span of characters representing the SUFRAMA registration to validate.</param>
        /// <returns><see langword="true"/> if the SUFRAMA registration is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsSuframa(ReadOnlySpan<char> suframaInsc)
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
        public static bool IsTituloEleitor(string? tituloEleitor) => !tituloEleitor.IsBlank() && IsTituloEleitor(tituloEleitor.AsSpan()); 

        /// <summary>
        /// Validates a Brazilian Voter Registration Card (Título de Eleitor).
        /// It ignores formatting characters and expects exactly 12 numeric digits.
        /// Uses the Module 11 validation rules specified by the TSE (Tribunal Superior Eleitoral).
        /// </summary>
        /// <param name="tituloEleitor">A read-only span of characters representing the Voter Registration Card to validate.</param>
        /// <returns><see langword="true"/> if the registration number is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsTituloEleitor(ReadOnlySpan<char> tituloEleitor)
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


        #endregion

    }
}
