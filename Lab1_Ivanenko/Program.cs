using System;
using System.Text;

namespace GameItemsDemo
{
    // Перечисление (enum) – редкость предмета
    public enum Rarity { Common, Rare, Epic, Legendary }

    // Вместо record — простой класс с Equals/GetHashCode/ToString
    public sealed class ItemId
    {
        public string Value { get; }

        public ItemId(string value)
        {
            Value = value ?? string.Empty;
        }

        public override string ToString() => Value;
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) =>
            obj is ItemId other && string.Equals(other.Value, Value, StringComparison.Ordinal);
    }

    // Интерфейс – "улучшаемые" предметы
    public interface IUpgradable { void Upgrade(); }

    // Пользовательские исключения
    public class NotEnoughLevelException : Exception
    {
        public string ItemName { get; }
        public int Required { get; }
        public int Actual { get; }

        public NotEnoughLevelException(string itemName, int required, int actual)
        {
            ItemName = itemName; Required = required; Actual = actual;
        }

        public override string Message =>
            $"Недостаточный уровень для использования «{ItemName}»: требуется {Required}, у игрока {Actual}.";
    }

    public class InventoryFullException : Exception
    {
        public override string Message => "Инвентарь переполнен (нет свободных слотов).";
    }

    // Абстрактный базовый класс
    public abstract class Item
    {
        public ItemId Id { get; }
        public string Name { get; }
        public Rarity Rarity { get; }
        public int RequiredLevel { get; }

        protected Item(ItemId id, string name, Rarity rarity, int requiredLevel)
        {
            Id = id; Name = name; Rarity = rarity; RequiredLevel = requiredLevel;
        }

        public abstract void Use(Player player);

        public override string ToString() => $"{Name} [{Rarity}] (треб. ур. {RequiredLevel})";
        public override bool Equals(object obj) => obj is Item other && Equals(Id, other.Id);
        public override int GetHashCode() => Id.GetHashCode();
    }

    public class Weapon : Item, IUpgradable
    {
        public int Damage { get; private set; }
        public int Level { get; private set; } = 1;

        public Weapon(ItemId id, string name, Rarity rarity, int requiredLevel, int damage)
            : base(id, name, rarity, requiredLevel) { Damage = damage; }

        public override void Use(Player player)
        {
            if (player.Level < RequiredLevel)
                throw new NotEnoughLevelException(Name, RequiredLevel, player.Level);

            player.EquipWeapon(this);
            Console.WriteLine($"Игрок экипировал оружие: {this}");
        }

        public void Upgrade()
        {
            Level++; Damage += 1;
            Console.WriteLine($"Оружие «{Name}» улучшено до ур.{Level}, урон теперь {Damage}.");
        }

        public override string ToString() => base.ToString() + $" | урон: {Damage}, ур.предм.: {Level}";
    }

    public class Armor : Item, IUpgradable
    {
        public int Defense { get; private set; }

        public Armor(ItemId id, string name, Rarity rarity, int requiredLevel, int defense)
            : base(id, name, rarity, requiredLevel) { Defense = defense; }

        public override void Use(Player player)
        {
            if (player.Level < RequiredLevel)
                throw new NotEnoughLevelException(Name, RequiredLevel, player.Level);

            player.EquipArmor(this);
            Console.WriteLine($"Игрок надел броню: {this}");
        }

        public void Upgrade()
        {
            Defense += 1;
            Console.WriteLine($"Броня «{Name}» улучшена, защита теперь {Defense}.");
        }

        public override string ToString() => base.ToString() + $" | защита: {Defense}";
    }

    public class Potion : Item
    {
        public int HealAmount { get; }

        public Potion(ItemId id, string name, Rarity rarity, int requiredLevel, int healAmount)
            : base(id, name, rarity, requiredLevel) { HealAmount = healAmount; }

        public override void Use(Player player)
        {
            if (player.Level < RequiredLevel)
                throw new NotEnoughLevelException(Name, RequiredLevel, player.Level);

            player.Heal(HealAmount);
            Console.WriteLine($"Игрок выпил зелье: {this}, восстановлено {HealAmount} HP.");
        }

        public override string ToString() => base.ToString() + $" | лечение: {HealAmount}";
    }

    public class Player
    {
        private readonly Item[] _inventory = new Item[5];

        public string Name { get; }
        public int Level { get; private set; }
        public int Health { get; private set; }
        public int Attack { get; private set; }
        public int Defense { get; private set; }

        public Player(string name, int level, int health = 30, int attack = 3, int defense = 0)
        {
            Name = name; Level = level; Health = health; Attack = attack; Defense = defense;
        }

        public void PickUp(Item item)
        {
            for (int i = 0; i < _inventory.Length; i++)
            {
                if (_inventory[i] == null)
                {
                    _inventory[i] = item;
                    Console.WriteLine($"Подобран предмет: {_inventory[i]} (слот {i})");
                    return;
                }
            }
            throw new InventoryFullException();
        }

        public void UseItem(int index)
        {
            if (index < 0 || index >= _inventory.Length) throw new IndexOutOfRangeException();

            var item = _inventory[index];
            if (item == null) { Console.WriteLine("В этом слоте нет предмета."); return; }

            item.Use(this);
            if (item is Potion) _inventory[index] = null;
        }

        public void Heal(int value)
        {
            Health += value;
            if (Health > 100) Health = 100;
            if (Health < 0) Health = 0;
        }

        public void EquipArmor(Armor armor) { Defense += armor.Defense; }
        public void EquipWeapon(Weapon weapon) { Attack += weapon.Damage; }

        public override string ToString() => $"{Name}: ур.{Level}, HP {Health}, ATK {Attack}, DEF {Defense}";
    }

    public class Enemy
    {
        public string Type { get; }
        public int Health { get; set; }
        public int Attack { get; }

        public Enemy(string type, int health, int attack)
        {
            Type = type; Health = health; Attack = attack;
        }

        public override string ToString() => $"{Type}: HP {Health}, ATK {Attack}";
    }

    public class Game
    {
        private static class Logger { public static void Info(string t) => Console.WriteLine(t); }
        private readonly Random _rng = new Random();

        public void Run()
        {
            Logger.Info("=== Мини-демо: Игровые предметы (ООП + SOLID) ===");

            var zone = _rng.Next(2) == 0 ? "Лес" : "Пещера";
            Logger.Info($"Локация: {zone}, время: {DateTime.Now:T}\n");

            var player = new Player("Новичок", _rng.Next(1, 4));
            Logger.Info($"Игрок: {player}\n");

            var itemsToSpawn = new Item[]
            {
                new Weapon(new ItemId(Guid.NewGuid().ToString()), "Ржавый меч", Rarity.Common,    1, 3),
                new Armor (new ItemId(Guid.NewGuid().ToString()), "Кожаная броня", Rarity.Common, 1, 2),
                new Potion(new ItemId(Guid.NewGuid().ToString()), "Малое зелье", Rarity.Common,   1, 5),
                new Weapon(new ItemId(Guid.NewGuid().ToString()), "Стальной топор", Rarity.Rare,  2, 5),
                new Armor (new ItemId(Guid.NewGuid().ToString()), "Кольчуга", Rarity.Rare,        2, 3),
            };

            foreach (var it in itemsToSpawn)
            {
                try { player.PickUp(it); }
                catch (InventoryFullException ex) { Logger.Info(ex.Message); }
            }

            var enemy = _rng.Next(2) == 0
                ? new Enemy("Волк", 12, 3)
                : new Enemy("Гоблин", 14, 2);

            Logger.Info($"Появляется враг: {enemy}\n");

            int attempts = _rng.Next(2, 4);
            for (int t = 0; t < attempts; t++)
            {
                int slot = _rng.Next(0, 5);
                try
                {
                    Logger.Info($"Попытка использовать предмет из слота {slot}...");
                    player.UseItem(slot);

                    if (_rng.Next(3) == 0)
                    {
                        var upgradable = itemsToSpawn[_rng.Next(itemsToSpawn.Length)] as IUpgradable;
                        if (upgradable != null) upgradable.Upgrade();
                    }
                }
                catch (NotEnoughLevelException ex)
                {
                    Logger.Info(ex.Message);
                }
                catch (IndexOutOfRangeException)
                {
                    Logger.Info("[Непредвиденная ошибка] IndexOutOfRangeException: индекс вне диапазона. Продолжаем.");
                }
            }

            Logger.Info("\nНачинается бой!");
            for (int round = 1; round <= 3 && player.Health > 0 && enemy.Health > 0; round++)
            {
                Logger.Info($"\nРаунд {round}:");

                int damageToEnemy = Math.Max(1, player.Attack - _rng.Next(0, 2));
                enemy.Health -= damageToEnemy;
                Logger.Info($"Игрок наносит {damageToEnemy} урона. {enemy}");
                if (enemy.Health <= 0) break;

                int damageToPlayer = Math.Max(1, enemy.Attack - player.Defense);
                player.Heal(-damageToPlayer);
                Logger.Info($"Враг наносит {damageToPlayer} урона. Игрок: {player}");

                if (player.Health <= 15 && _rng.Next(2) == 0)
                {
                    try
                    {
                        Logger.Info("Игрок ищет зелье в слоте 2...");
                        player.UseItem(2);
                        Logger.Info($"Состояние игрока: {player}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Info($"Зелье не использовано: {ex.Message}");
                    }
                }
            }

            Logger.Info("\nИтог:");
            if (enemy.Health <= 0) Logger.Info($"Победа! Враг повержен. Игрок: {player}");
            else if (player.Health <= 0) Logger.Info("Поражение. Игрок пал.");
            else Logger.Info("Бой завершился ничьей (оба устали).");

            Logger.Info("\n=== Конец демо ===");
        }
    }

    public class Program
    {
        public static void Main()
        {
            // ВКЛЮЧАЕМ РУССКИЙ В ВЫВОДЕ/ВВОДЕ
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            var game = new Game();
            game.Run();

            Console.WriteLine("\nНажмите Enter для выхода...");
            Console.ReadLine();
        }
    }
}
