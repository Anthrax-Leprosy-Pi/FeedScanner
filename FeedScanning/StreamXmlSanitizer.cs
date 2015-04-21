// Type: FeedSkimming.StreamXmlSanitizer
// Assembly: FeedSkimmer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 68D455E3-3B61-4386-A8E3-410E9F903232
// Assembly location: D:\!Prasse\Downloads\FeedSkimmer\FeedSkimmer.exe

using System;
using System.IO;
using System.Text;

namespace FeedScanning
{
  public class StreamXmlSanitizer : StreamReader
  {
    private const int EOF = -1;

    public StreamXmlSanitizer(Stream streamToSanitize)
      : base(streamToSanitize, true)
    {
    }

    public static bool IsLegalXmlChar(string xmlVersion, int character)
    {
      switch (xmlVersion)
      {
        case "1.1":
          return character > 8 && character != 11 && character != 12 && ((character < 14 || character > 31) && (character < (int) sbyte.MaxValue || character > 132)) && (character < 134 || character > 159) && character <= 1114111;
        case "1.0":
          return character == 9 || character == 10 || character == 13 || (character >= 32 && character <= 55295 || character >= 57344 && character <= 65533) || character >= 65536 && character <= 1114111;
        default:
          throw new ArgumentOutOfRangeException("xmlVersion", string.Format("'{0}' is not a valid XML version.", new object[0]));
      }
    }

    public static bool IsLegalXmlChar(int character)
    {
      return StreamXmlSanitizer.IsLegalXmlChar("1.0", character);
    }

    public override int Read()
    {
      int character;
      do
        ;
      while ((character = base.Read()) != -1 && !StreamXmlSanitizer.IsLegalXmlChar(character));
      return character;
    }

    public override int Peek()
    {
      int character;
      do
      {
        character = base.Peek();
      }
      while (!StreamXmlSanitizer.IsLegalXmlChar(character) && (character = base.Read()) != -1);
      return character;
    }

    public override int Read(char[] buffer, int index, int count)
    {
      if (buffer == null)
        throw new ArgumentNullException("buffer");
      if (index < 0)
        throw new ArgumentOutOfRangeException("index");
      if (count < 0)
        throw new ArgumentOutOfRangeException("count");
      if (buffer.Length - index < count)
        throw new ArgumentException();
      int num1 = 0;
      do
      {
        int num2 = this.Read();
        if (num2 == -1)
          return num1;
        buffer[index + num1++] = (char) num2;
      }
      while (num1 < count);
      return num1;
    }

    public override int ReadBlock(char[] buffer, int index, int count)
    {
      int num1 = 0;
      int num2;
      do
      {
        num1 += num2 = this.Read(buffer, index + num1, count - num1);
      }
      while (num2 > 0 && num1 < count);
      return num1;
    }

    public override string ReadLine()
    {
      StringBuilder stringBuilder = new StringBuilder();
      int num;
      while (true)
      {
        num = this.Read();
        switch (num)
        {
          case -1:
            goto label_1;
          case 10:
          case 13:
            goto label_4;
          default:
            stringBuilder.Append((char) num);
            continue;
        }
      }
label_1:
      if (stringBuilder.Length > 0)
        return ((object) stringBuilder).ToString();
      else
        return (string) null;
label_4:
      if (num == 13 && this.Peek() == 10)
        this.Read();
      return ((object) stringBuilder).ToString();
    }

    public override string ReadToEnd()
    {
      char[] buffer = new char[4096];
      StringBuilder stringBuilder = new StringBuilder(4096);
      int charCount;
      while ((charCount = this.Read(buffer, 0, buffer.Length)) != 0)
        stringBuilder.Append(buffer, 0, charCount);
      return ((object) stringBuilder).ToString();
    }
  }
}
