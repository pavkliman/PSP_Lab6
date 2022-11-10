using System.Net;
using System.Text;
using System.Web;

string address = "https://localhost:8088/";
var listener = new HttpListener();
listener.Prefixes.Add(address);
listener.Start();
Console.WriteLine("Ожидание подключений...");
while (true)
{
    ThreadPool.QueueUserWorkItem(AcceptClient);
}

void AcceptClient(object o)
{
    HttpListenerContext context = listener.GetContext();
    HttpListenerRequest request = context.Request;
    if (request.RawUrl[1..] == "favicon.ico")
    {
        Console.WriteLine($"Клиент принят в потоке с ID: {Environment.CurrentManagedThreadId}");
    }

    if (context.Request.HttpMethod == "GET")
    {
        string responseString = "<html><head><meta charset='utf8'></head><body>" +
            "<form action='/calculate' method='post'>" +
            "<input type='text' name='expression'/>" +
            "<input type='submit' name='calculate' value='Calculate'/>" +
            "</form></body></html>";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        var response = context.Response;
        response.ContentLength64 = buffer.Length;
        var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }
    else
    {
        var requestString = "";
        byte[] buffer = null;
        int count = 0;
        do
        {
            buffer = new byte[1024];
            count = request.InputStream.Read(buffer, 0, 1024);
            requestString += HttpUtility.UrlDecode(buffer, Encoding.UTF8);
        }
        while (request.InputStream.CanRead && count > 0);
        requestString = requestString.Replace("\0", string.Empty);
        requestString = new string(requestString[11..].Reverse().ToArray());
        requestString = new string(requestString[20..].Reverse().ToArray());
        string responseString = "<html><head><meta charset='utf8'></head><body>" +
            $"<div>Результат = {Calculate(requestString)}<div/>" +
            "</form></body></html>";
        buffer = Encoding.UTF8.GetBytes(responseString);
        var response = context.Response;
        response.ContentLength64 = buffer.Length;
        var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }
}

#region обратная польская запись
//Метод возвращает true, если проверяемый символ - разделитель ("пробел" или "равно")
static bool IsDelimeter(char c)
{
    if ((" =".IndexOf(c) != -1))
        return true;
    return false;
}
//Метод возвращает true, если проверяемый символ - оператор
static bool IsOperator(char с)
{
    if (("+-/*()".IndexOf(с) != -1))
        return true;
    return false;
}
//Метод возвращает приоритет оператора
static byte GetPriority(char s)
{
    switch (s)
    {
        case '(': return 0;
        case ')': return 1;
        case '+': return 2;
        case '-': return 3;
        case '*': return 4;
        case '/': return 5;
        default: return 6;
    }
}

//"Входной" метод класса
static double Calculate(string input)
{
    string output = GetExpression(input); //Преобразовываем выражение в постфиксную запись
    double result = Counting(output); //Решаем полученное выражение
    return result; //Возвращаем результат
}

static string GetExpression(string input)
{
    string output = string.Empty; //Строка для хранения выражения
    Stack<char> operStack = new Stack<char>(); //Стек для хранения операторов

    for (int i = 0; i < input.Length; i++) //Для каждого символа в входной строке
    {
        //Разделители пропускаем
        if (IsDelimeter(input[i]))
            continue; //Переходим к следующему символу

        //Если символ - цифра, то считываем все число
        if (Char.IsDigit(input[i])) //Если цифра
        {
            //Читаем до разделителя или оператора, что бы получить число
            while (!IsDelimeter(input[i]) && !IsOperator(input[i]))
            {
                output += input[i]; //Добавляем каждую цифру числа к нашей строке
                i++; //Переходим к следующему символу

                if (i == input.Length) break; //Если символ - последний, то выходим из цикла
            }

            output += " "; //Дописываем после числа пробел в строку с выражением
            i--; //Возвращаемся на один символ назад, к символу перед разделителем
        }

        //Если символ - оператор
        if (IsOperator(input[i])) //Если оператор
        {
            if (input[i] == '(') //Если символ - открывающая скобка
                operStack.Push(input[i]); //Записываем её в стек
            else if (input[i] == ')') //Если символ - закрывающая скобка
            {
                //Выписываем все операторы до открывающей скобки в строку
                char s = operStack.Pop();

                while (s != '(')
                {
                    output += s.ToString() + ' ';
                    s = operStack.Pop();
                }
            }
            else //Если любой другой оператор
            {
                if (operStack.Count > 0) //Если в стеке есть элементы
                    if (GetPriority(input[i]) <= GetPriority(operStack.Peek())) //И если приоритет нашего оператора меньше или равен приоритету оператора на вершине стека
                        output += operStack.Pop().ToString() + " "; //То добавляем последний оператор из стека в строку с выражением

                operStack.Push(char.Parse(input[i].ToString())); //Если стек пуст, или же приоритет оператора выше - добавляем операторов на вершину стека

            }
        }
    }

    //Когда прошли по всем символам, выкидываем из стека все оставшиеся там операторы в строку
    while (operStack.Count > 0)
        output += operStack.Pop() + " ";

    return output; //Возвращаем выражение в постфиксной записи
}

static double Counting(string input)
{
    double result = 0; //Результат
    Stack<double> temp = new Stack<double>(); //Dhtvtyysq стек для решения

    for (int i = 0; i < input.Length; i++) //Для каждого символа в строке
    {
        //Если символ - цифра, то читаем все число и записываем на вершину стека
        if (Char.IsDigit(input[i]))
        {
            string a = string.Empty;

            while (!IsDelimeter(input[i]) && !IsOperator(input[i])) //Пока не разделитель
            {
                a += input[i]; //Добавляем
                i++;
                if (i == input.Length) break;
            }
            temp.Push(double.Parse(a)); //Записываем в стек
            i--;
        }
        else if (IsOperator(input[i])) //Если символ - оператор
        {
            //Берем два последних значения из стека
            double a = temp.Pop();
            double b = temp.Pop();

            switch (input[i]) //И производим над ними действие, согласно оператору
            {
                case '+': result = b + a; break;
                case '-': result = b - a; break;
                case '*': result = b * a; break;
                case '/': result = b / a; break;
            }
            temp.Push(result); //Результат вычисления записываем обратно в стек
        }
    }
    return temp.Peek(); //Забираем результат всех вычислений из стека и возвращаем его
}

#endregion