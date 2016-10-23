public interface IPrefType
{
    string ToUniqueString();

    void FromUniqueString(string sstr);

    void Load();
}