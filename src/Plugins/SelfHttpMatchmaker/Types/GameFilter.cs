using System.Runtime.Serialization;

namespace SelfHttpMatchmaker.Types;

public class GameFilter
{
    public required string OptionType { get; set; }

    public required string Key { get; set; }
    
    public required string SubFilterString { get; set; }

    [OnSerializing]
    internal void OnSerializing(StreamingContext context)
    {
        
    }

    private static uint ComputeStringHash(string s)
    {
        return s.Aggregate(2166136261U, (current, t) => (t ^ current) * 16777619U);
    }
    
    private ISubFilter ResolveSubFilter(string type, string filterString)
    {
        var num = ComputeStringHash(type);
        switch (num)
        {
            case <= 2515107422U when num != 108289031U:
            {
                if (num != 709505714U)
                {
                        if (num == 2515107422U)
                        {
                            if (type == "int")
                            {
                                return JsonConvert.DeserializeObject<IntGameFilter>(filterString);
                            }
                        }
                    }
                    else if (type == "platform")
                    {
                        return JsonConvert.DeserializeObject<PlatformGameFilter>(filterString);
                    }

                    break;
                }
                case <= 2515107422U when type == "cat":
                    return JsonConvert.DeserializeObject<CategorizedGameFilter>(filterString);
                case <= 2515107422U:
                    break;
                case <= 3147117720U when num != 2722888107U:
                {
                    if (num == 3147117720U)
                    {
                        if (type == "languages")
                        {
                            return JsonConvert.DeserializeObject<LanguageFilter>(filterString);
                        }
                    }

                    break;
                }
                case <= 3147117720U when type == "chat":
                    return JsonConvert.DeserializeObject<ChatModeGameFilter>(filterString);
                case <= 3147117720U:
                    break;
                default:
                {
                    if (num != 3365180733U)
                    {
                        if (num == 3751997361U)
                        {
                            if (type == "map")
                            {
                                return JsonConvert.DeserializeObject<MapGameFilter>(filterString);
                            }
                        }
                    }
                    else if (type == "bool")
                    {
                        return JsonConvert.DeserializeObject<BoolGameFilter>(filterString);
                    }

                    break;
                }
            }
        return null;
    }
}
