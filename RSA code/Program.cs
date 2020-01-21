using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

using Mpir.NET;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            //Stopwatch sW = new Stopwatch();
            //sW.Start();
            //sW.Stop();
            //Console.WriteLine("\nTime Elapsed: {0}", sW.Elapsed); //Used to print time results where appropriate.

            mpz_t p, q, e, n, t, d, c, m;
            ulong keySize;

            keySize = ChooseKeySize();
            p = GenerateRandPrimes(keySize);
            q = GenerateRandPrimes(keySize);
            n = FindModulus(p,q);
            t = FindTotient(p,q);
            e = ChooseE(n);
            d = ExtendedGCD(e,t);
            m = Message();
            c = Encryption(m, e, n);
            ToFile(e, n, d, c);
            Decryption(c, d, n);

            Console.Read();

        }

        static ulong ChooseKeySize() //Gathering the user input for desired key length.
        {
            while(true)
            {
                string f;
                Console.WriteLine("Enter your desired key size, choose: 512, 1024, 2048 or 4096. ");
                f = Console.ReadLine();
                ulong g = Convert.ToUInt64(f);

                if (g != 512 && g != 1024 && g != 2048 && g != 4096) //If not these values then loop back again until correct key size is chosen.
                {
                    Console.WriteLine("Invalid key size.");
                }
                else
                {
                    return g;
                }
            }
        }

        static mpz_t Message()               //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/how-to-convert-between-hexadecimal-strings-and-numeric-types//    //https://machinecognitis.github.io/Math.Gmp.Native/html/8c8c1e55-275f-cff8-2152-883a4eaa163c.htm//
        {
            string appPath = Application.StartupPath;
            string filepath = "testdata.txt"; //Gets file path from folder
            string fullPath = Path.Combine(appPath, filepath); // combines string into path

            string h = File.ReadAllText(fullPath); //Read the file
            byte[] msg = System.Text.Encoding.UTF8.GetBytes(h); //Get file byte size
            int length = h.Length; //Convert string to int for later conversion.
            uint count = Convert.ToUInt32(length); //Converts to uint data type to be able to convert to mpz_t further on.

            mpz_t y = new mpz_t();
            mpir.Mpir_mpz_import(y, count, 1, sizeof(Byte), 0, 0, msg); //This converts 'y' to an mpz_t data type which holds the message.
            mpir.mpz_get_string(16, y); //Converts to base 16 or hexadecimal

            return y; //Returns 'y' that has been converted to hexadecimal.
        }

        static mpz_t GenerateRandPrimes(ulong KEY_SIZE)
        {
            byte[] keySize = new byte[KEY_SIZE];
            Random r = new Random();
            int h = keySize.Length/8; //dividing the KEY_SIZE length by 8 intially allows the for loop to generate numbers of a length of 8.
            string s = string.Empty;

            for(int i =0; i<h/8; i++) //You want the for loop to generate number a length of 8 equivalent of h/8.
            {
                s = string.Concat(s, r.Next(10000000,99999999).ToString()); //generating a number a length of 8 to String.
            }

            byte[] key = Encoding.UTF8.GetBytes(s); //convert from String to byte[].
            int length = s.Length;
            uint count = Convert.ToUInt32(length); //Convert length of String to Int32.

            mpz_t j = new mpz_t();
            mpir.Mpir_mpz_import(j, count, 1, sizeof(Byte), 0, 0, key); //mpz_t variables can be converted to and from arbitary words of binary data. 
                                                                        //This converts 'j' to an mpz_t data type which holds the generated key.

            bool v = IsPrime(j); //Check if 'j' is prime.

            if(v == true) //if 'j' turns out to be a prime number then return it.
            {
                return j;
            }

            else //if 'j' isn't prime do this.
            {
                NextPrime(j); //Call and generate a prime greater than 'j'.
                IsPrime(j); //Check whether the newly generated 'nextprime' 'j' is actually prime.
                return j;
            }
        }

        static bool IsPrime(mpz_t n)
        {
            mpz_t u = 40; //The amount of primality tests going to be run which determines the accuracy of the test. p = probability therefore p^u(1/4)^u.

            if(n < 2) //if number is less than 2, it's of no use so return false.
            {
                return false;
            }
            if(n != 2 && n % 2 == 0) //if number is not equal to 2 and not a multiplication of 2 therefore removes all positive integers.
            {
                return false;
            }

            mpz_t s = new mpz_t();
            s = n - 1; //to get this far the number must be odd so minus 1 to make it even.

            while (s % 2 == 0) //extract all multiples of 2 from s.
            {
                s = s >> 1; //s right shifted by 1.
            }

            for(mpz_t  i = 0; i < u; i++) //repeat by number of test times.
            {
                mpz_t a = new mpz_t();
                gmp_randstate_t state = new gmp_randstate_t();

                mpir.mpz_urandomm(a, state, n);
                a = a - 1;

                mpz_t brief = new mpz_t();
                brief = s;

                mpz_t x = new mpz_t();
                x = a.PowerMod(brief, n); //compute a^brief MOD n.

                while(brief != n - 1 && x != 1 && x != n - 1) //repeatedly square so that x^brief = (x^brief)^2 = 1.
                {
                    x = (x * x) % n; 
                    brief = brief * 2;
                }

                if(x != n - 1 && brief % 2 == 0)
                {
                    return false; 
                }
            }
            return true;  
        }

        static mpz_t NextPrime(mpz_t a)
        {
            bool y;
            while (true)
            {
                y = IsPrime(a);
                if(y == true) //If True return a, continue application.
                {
                    return a;
                }
                else
                {
                    a++; //if !true then increment +1 to variable 'a' and check if it is prime.
                }
            }
        }

        static mpz_t FindModulus(mpz_t p, mpz_t q)
        {
            mpz_t n = p*q;
            return n;
        }

        static mpz_t FindTotient(mpz_t p, mpz_t q)
        {
            mpz_t t = (p - 1) * (q - 1);
            return t;
        }

        static mpz_t ChooseE(mpz_t n)
        {
            string f;
            mpz_t e = new mpz_t();
            Console.WriteLine("Enter your public exponent: ");
            f = Console.ReadLine();
            mpir.mpz_set_str(e, f, 10);  //convert user input to mpz_t data type under base 10.

            mpz_t q = new mpz_t();
            bool y = IsPrime(e); //Check whether public key exponent is prime.

            if (e > n || e % 2 == 0 || y == false)  //PKCS#1 v2.1 states that the public exponent can be the same magnitude as the modulus length. Other standards may conflict this requirement such as FIPS 186-4 require 2^16 < public exponent < 2^256.
            {
                Console.WriteLine("\nInvalid public exponent.");
                NextPrime(e); //Generate next prime of the user input if user input isn't a prime number.
                Console.WriteLine("Nearest prime has been generated for you: {0}\n", e);
                return e;
            }

            return e;
        }

        static mpz_t ExtendedGCD(mpz_t e, mpz_t t)
        {
            mpz_t r = new mpz_t();
            mpz_t s = new mpz_t();
            mpz_t g = new mpz_t();

            mpir.mpz_gcdext(g, r, s, e, t); //Greatest common divisor of a set of given numbers and the coefficients of Bezout's identity (Covered in the report accompanying this code).

            g = r + t; //Add the coefficeint to phi(n).
            return g;
        }

        static mpz_t AddPadding(mpz_t m, mpz_t n)
        {
            string source = mpir.mpz_get_string(10, m);
            int mlen = source.Length; //message length

            Random r = new Random();
            int rand = r.Next(source.Length); //Randomise a number within the boundaries of source.length.

            string result = source.PadLeft(mlen * 2, source[rand]);  //Pad message with the array of source.length
            mpz_t z = new mpz_t();
            mpir.mpz_set_str(z, result, 10); //Convert result into mpz_t data type 'z'.

            return z;
        }

        static mpz_t RemovePadding(mpz_t m, mpz_t n)
        {
            string source = mpir.mpz_get_string(10, m);
            int mlen = source.Length;

            int hlfPad = mlen / 2; //Due to padding the message double the message length, find half that.

            string result = source.Substring(hlfPad, source.Length - hlfPad); //subtract half of the message length at the front of the message due to padding left initially.
            mpz_t x = new mpz_t();
            mpir.mpz_set_str(x, result, 10); //Convert result into mpz_t data type 'x'.
            return x;
        }

        static mpz_t Encryption(mpz_t m, mpz_t e, mpz_t n)                  //https://www.infotechno.net/some-of-the-finer-details-of-rsa-public-key-cryptography         //http://doctrina.org/How-RSA-Works-With-Examples.html  */
        {
            mpz_t W = 0;
            mpir.mpz_powm(W, m, e, n); //Calling power modulus calculation using the message, modulus and public key.

            mpz_t C = W % n; //calculating remainder of W and modulus which gives us the ciphertext.
            C = AddPadding(m, n);
            return C;
        }

        static string Decryption(mpz_t c, mpz_t d, mpz_t n)
        {
            mpz_t M = RemovePadding(c, n);

            /*mpz_t X = 0;
            mpir.mpz_powm(X,RmvPad,d,n); //Calling power modulus calculation using the ciphertext, modulus and private key.
            mpz_t M = X % n;*/      //calculating remainder of X and modulus which gives us the initial message in decimal format.


            string h = mpir.mpz_get_string(16, M); //converting to base 16 (hex) //Convert Decimal into Hexadecimal.
            string g = ""; //Ready to write to string.


            for (int i=0; i < h.Length / 2; i++) //https://codereview.stackexchange.com/questions/97950/conversion-of-hexadecimal-string-to-string
            {
                string hChar = h.Substring(i * 2, 2); //retrieving a substring from this instance.
                int hValue = Convert.ToInt32(hChar, 16);    //This for loop converts Hexadecimal into string!
                g = g + Char.ConvertFromUtf32(hValue); 
            }

            Console.WriteLine("\nDecrypted\n\t" + "{0}", g); //Prints out the conversion result.
            return g;
        }

        static void ToFile(mpz_t e, mpz_t n, mpz_t d, mpz_t c)
        {
            string appPath = Application.StartupPath;
            string PubKeyFilePath = "PubKey.txt";
            string PrivKeyFilePath = "PrivKey.txt";
            string EncryptedFilePath = "EncryptFile.txt";

            string PubKeyFile = Path.Combine(appPath, PubKeyFilePath);
            string PrivKeyFile = Path.Combine(appPath, PrivKeyFilePath);
            string EncryptedFile = Path.Combine(appPath, EncryptedFilePath);

            if (File.Exists(PrivKeyFile))
            {
                File.Delete(PrivKeyFile);
            }

            if (File.Exists(EncryptedFile))
            {
                File.Delete(EncryptedFile);
            }

            if (!File.Exists(PubKeyFile)) //If file doesn't exist, run.
            {
                using (StreamWriter PubKeyFW = File.CreateText(PubKeyFile)) //Creates file and prints public key to it.
                {
                    string Q,U;
                    Q =  mpir.mpz_get_string(16, e);
                    PubKeyFW.Write("-----BEGIN PUBLIC KEY-----");
                    PubKeyFW.Write("\n{0}", Q);

                    U = mpir.mpz_get_string(16, n);
                    PubKeyFW.Write("{0}", U);
                    PubKeyFW.Write("\n-----END PUBLIC KEY-----");
                }
            }

            if (!File.Exists(PrivKeyFile)) //If file doesn't exist, run. 
            {
                using (StreamWriter PrivKeyFW = File.CreateText(PrivKeyFile)) //Creates file and prints private key to it.
                {
                    string d1;
                    d1 = mpir.mpz_get_string(16, d);
                    PrivKeyFW.Write("-----BEGIN PRIVATE KEY-----");
                    PrivKeyFW.Write("\n{0}", d1);
                    PrivKeyFW.Write("\n-----END PRIVATE KEY-----");
                }
            }

            if (!File.Exists(EncryptedFile))
            {
                using (StreamWriter PrivKeyFW = File.CreateText(EncryptedFile)) //Prints encrypted data to file.
                {
                    string c1;
                    c1 = mpir.mpz_get_string(16, c);
                    PrivKeyFW.WriteLine("\nEncrypted\n\t" + "{0}", c1); //This was commented out whilst testing as printing out the encrypted file added to the time.
                }
            }

            using (StreamReader EncryptedFileRead = File.OpenText(EncryptedFile)) //Reads encrypted file
            {
                string c1;

                while((c1 = EncryptedFileRead.ReadLine()) != null)
                {
                    Console.WriteLine(c1);
                    string line = EncryptedFileRead.ReadToEnd();
                    Console.WriteLine(line);
                }
            }
        }
    }
}