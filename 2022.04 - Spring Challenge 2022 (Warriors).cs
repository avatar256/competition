using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class MainApp
{
	static void Main(string[] args)
	{
		Game game = new Game();

		while (true)
		{
			game.NextTurn();
		}
	}
}


public class Game
{
	public static Game This						= null;
	public int Turn								= 0;
	public List<Entity> Entities				= new List<Entity>();
	public List<Entity> EntitiesRemoved			= new List<Entity>();
	public Player PlayerMy						= null;
	public Player PlayerEnemy					= null;


	public Game()
	{
		This = this;
		Init();
	}


	public void Init()
	{
		string[] inputs = Console.ReadLine().Split(' ');
		int baseX = int.Parse(inputs[0]); // The corner of the map representing your base
		int baseY = int.Parse(inputs[1]);
		int heroesPerPlayer = int.Parse(Console.ReadLine()); // Always 3
		 
		Player.HeroesPerPlayer = heroesPerPlayer;
		PlayerMy = new Player(ETeam.My, 0, new PointI(baseX, baseY));
		if ( baseX == 0 )
			PlayerEnemy = new Player(ETeam.Enemy, 1, new PointI(17630, 9000));
		else
			PlayerEnemy = new Player(ETeam.Enemy, 1, new PointI(0, 0));
	}


	public void NextTurn()
	{
		double time1 = (DateTime.Now.Ticks / (double)TimeSpan.TicksPerMillisecond);
		Turn++;
		
		PlayerMy.NextTurn();
		PlayerEnemy.NextTurn();
		UpdateEntities();
		double time2 = (DateTime.Now.Ticks / (double)TimeSpan.TicksPerMillisecond);
		
		List<String> commands = GetCommands();
		double time3 = (DateTime.Now.Ticks / (double)TimeSpan.TicksPerMillisecond);
		double lastParseTime = time2 - time1;
		double lastLogicTime = time3 - time2;
		Console.Error.WriteLine( $"Turn {Turn}, TimeLogic {(int)lastLogicTime}, TimeParse {(int)lastParseTime}" );

		for ( int i = 0; i < commands.Count; i++ )
			Console.WriteLine( commands[i] );
	}


	private void UpdateEntities()
	{
		for ( int i = 0; i < Entities.Count; i++ )
			Entities[i].NeedDelete = true;

		int entityCount = int.Parse(Console.ReadLine());
		for (int i = 0; i < entityCount; i++)
		{
			string[] inputs = Console.ReadLine().Split(' ');
			int entityType = int.Parse(inputs[1]);
			int id = int.Parse(inputs[0]);

			Entity ent = FindOrCreateEntity(entityType, id);
			if (ent == null)
				continue;
			if (!ent.IsInitialized)
			{
				if (ent.Type == EEntityType.Unknown)
					Console.Error.WriteLine($"UNKNOWN: {String.Join(" ", inputs)}");
				ent.Init(inputs);
			}
			ent.UpdateParams(inputs);
		}

		for ( int i = Entities.Count-1; i >= 0; i-- )
		{
			if ( Entities[i].CanDelete() )
				Entities[i].Delete();
			else
				Console.Error.WriteLine( Entities[i].ToString() );
		}
	}
	
	
	private List<String> GetCommands()
	{
		List<String> commands = new List<string>();
		commands.AddRange( PlayerMy.GetCommands() );
		return commands;
	}


	public Player GetPlayer( int id )
	{
		if ( PlayerMy.Id == id )
			return PlayerMy;
		if ( PlayerEnemy.Id == id )
			return PlayerEnemy;

		throw new Exception("not found player " + id);
	}


	private Entity FindOrCreateEntity( int type, int id )
	{
		for( int i = Entities.Count-1; i >= 0; i-- )
		{
			if ( Entities[i].Id == id )
				return Entities[i];
		}

		return Entity.CreateFromType( type, id );
	}
	

	public Entity FindById( int id, bool includeDestroyed = false )
	{
		if ( id < 0 )
			throw new Exception("need implement");
		for( int i = Entities.Count-1; i >= 0; i-- )
		{
			if ( Entities[i].Id == id )
				return Entities[i];
		}
		if (includeDestroyed)
		{
			for (int i = EntitiesRemoved.Count - 1; i >= 0; i--)
			{
				if (EntitiesRemoved[i].Id == id)
					return EntitiesRemoved[i];
			}
		}
		return null;
	}


	public List<T> FindAllByClass<T>( EEntityType type = EEntityType.Unknown, bool includeDestroyed = false )
	{
		List<T> found = new List<T>();
		for( int i = Entities.Count-1; i >= 0; i-- )
		{
			if ( Entities[i] is T && (type == EEntityType.Unknown || type == Entities[i].Type) )
				found.Add( (T)(object)Entities[i] );
		}
		if (includeDestroyed)
		{
			for (int i = EntitiesRemoved.Count - 1; i >= 0; i--)
			{
				if (EntitiesRemoved[i] is T && (type == EEntityType.Unknown || type == Entities[i].Type))
					found.Add((T)(object)EntitiesRemoved[i]);
			}
		}
		return found;
	}
	

	public List<Entity> FindAllByType( EEntityType type, bool includeDestroyed = false )
	{
		List<Entity> found = new List<Entity>();
		for( int i = Entities.Count-1; i >= 0; i-- )
		{
			if ( Entities[i].Type == type )
				found.Add( Entities[i] );
		}
		if (includeDestroyed)
		{
			for (int i = EntitiesRemoved.Count - 1; i >= 0; i--)
			{
				if (EntitiesRemoved[i].Type == type)
					found.Add( EntitiesRemoved[i] );
			}
		}
		return found;
	}
}


public enum EEntityType
{
	Monster = 0,
	Warrior,
	Unknown = -1
}


public enum ETeam
{
	My,
	Enemy,
	Unknown
}


public enum ECell
{
	Empty,
	Table,
	Player,
}


public enum EDir
{
	Up = 0,
	Right = 1,
	Down = 2,
	Left = 3
}

//################################# Player #################################//

public class Player
{
	public ETeam Team							= ETeam.Unknown;
	public int Id								= 0;
	public Game TheGame							= Game.This;
	public static int HeroesPerPlayer			= 0;
	public PointI BasePosition					= new PointI();
	public int BaseHealth						= 0;
	public int Mana								= 0;
	public List<Warrior> Warriors				= new List<Warrior>();
	public List<Monster> PracticeMonsters		= new List<Monster>();
	public List<Monster> DeadlyMonsters			= new List<Monster>();
	public bool NeedUpdateGuards				= true;
	public float AttackAngle					= 10f;
	public bool NeedProtectFromMagic			= false;
	

	public Player( ETeam team, int id, PointI basePos )
	{
		Team = team;
		Id = id;
		BasePosition = basePos;
	}
	
	
	public void NextTurn()
	{
		string[] inputs = Console.ReadLine().Split(' ');
		BaseHealth = int.Parse(inputs[0]);
		Mana = int.Parse(inputs[1]);

		Console.Error.WriteLine($"Player  team:{Team},  basePos:{BasePosition},  health:{BaseHealth},  mana:{Mana}");
	}


	public List<String> GetCommands()
	{
		SetWarriorDuties();

		List<String> commands = new List<string>();
		foreach( var war in Warriors )
			commands.Add( war.GetCommand() );
		return commands;
	}


	private void SetWarriorDuties()
	{
		// set guard position
		if ( NeedUpdateGuards )
		{
			NeedUpdateGuards = false;
			foreach( var war in Warriors )
				war.UpdateGuardPoint();
		}

		foreach( var war in Warriors )
		{
			war.Action = EWarriorAction.GoToGuardPosition;
			if ( TheGame.Turn >= 100 && war.WarriorNum == 0 )
				war.Action = EWarriorAction.Attacker;
		}

		// find all threat monsters
		List<Monster> monsters = TheGame.FindAllByClass<Monster>( EEntityType.Monster );
		DeadlyMonsters.Clear();
		PracticeMonsters.Clear();
		if ( monsters.Count == 0 )
			return;
		
		List<Monster> threats = new List<Monster>();
		foreach( var monster in monsters )
		{
			monster.DeadlyTurns = -1;
			monster.Priority = -1f;
			monster.SpellCast = ESpell.None;
			float monsterToBase = PointI.Distance( BasePosition, monster.Position, false );
			float threatRadius = 10000f;
			if ( monster.ThreatFor == this && monsterToBase <= threatRadius )
			{
				monster.Priority = 1000f + (threatRadius - monsterToBase) / threatRadius * 100f; // близко к базе prior+1100, далеко от базы prior+1000
				threats.Add( monster );
			}
			else// if ( monster.ThreatFor != null )
			{
				PracticeMonsters.Add( monster );
			}
		}

		threats.Sort( (m1, m2) => (int)(m2.Priority - m1.Priority) );
		Simulation sim = new Simulation();
		sim.SimulateMonsterWalk( threats );
		//Console.Error.WriteLine( sim );
		
		List<Warrior> freeWarriors = Warriors.GetRange( 0, Warriors.Count );
		for( int i = freeWarriors.Count-1; i >= 0; i-- )
		{
			if ( freeWarriors[i].Action == EWarriorAction.Attacker )
				freeWarriors.RemoveAt( i );
		}

		Monster target = sim.GetPriorityTarget();
		while( target != null )
		{
			Warrior nearestWarrior = null;
			float bestDist = 0;
			foreach( var warrior in freeWarriors )
			{
				float dist = PointI.Distance( warrior.Position, target.Position );
				if ( nearestWarrior == null || bestDist > dist )
				{
					nearestWarrior = warrior;
					bestDist = dist;
				}
			}

			// cast wind to protect base
			if ( nearestWarrior == null )
			{
				sim.SetAllDeadlyMark();
				foreach ( var threat in threats )
				{
					if ( threat.DeadlyTurns > 3 || threat.DeadlyTurns < 0 || threat.Shield > 0 || Mana < 10 )
						continue;

					freeWarriors = Warriors.GetRange( 0, Warriors.Count );
					nearestWarrior = null;
					bestDist = 0;
					foreach( var warrior in freeWarriors )
					{
						float dist = PointI.Distance( warrior.Position, threat.Position );
						if ( (nearestWarrior == null || bestDist > dist) && dist < 1280 )
						{
							nearestWarrior = warrior;
							bestDist = dist;
						}
					}
				}

				if ( nearestWarrior != null )
					nearestWarrior.Action = EWarriorAction.WindDefence;

				Console.Error.WriteLine( $"LOSE HEALTH from {target.Id}" );
				break;
			}

			WarriorState warriorState = sim.AddAttacker( nearestWarrior, target );
			nearestWarrior.Target = target;
			nearestWarrior.Action = EWarriorAction.DefendBase;
			nearestWarrior.TargetPosition = warriorState.PositionTarget.Clone();
			Console.Error.WriteLine( $"warrior{nearestWarrior.Id} attack{target.Id}" );
			//Console.Error.WriteLine( sim );
			target = sim.GetPriorityTarget();
			freeWarriors.Remove( nearestWarrior );
		}
	}
}

//################################# Entity #################################//

public class Entity
{
	public ETeam Team							= ETeam.Unknown;
	public EEntityType Type						= EEntityType.Unknown;
	public PointI Position						= new PointI();
	public PointI PrevPosition					= new PointI(int.MinValue,int.MinValue);
	public int Id								= -1;
	public Player Belongs						= null;
	public Game TheGame							= Game.This;
	public bool IsDeleted						= false;
	public bool NeedDelete						= false;
	public bool IsInitialized					= false;
	public int Shield							= 0;
	public bool IsMindControlled				= false;
	
	
	public Entity( int id, EEntityType type )
	{
		Id = id;
		Type = type;
		TheGame.Entities.Add( this );
	}


	public virtual void Delete()
	{
		IsDeleted = true;
		TheGame.Entities.Remove( this );
		TheGame.EntitiesRemoved.Add( this );
	}


	public static Entity CreateFromType( int type, int id )
	{
		switch( type )
		{
			case 0:		return new Monster( id );
			case 1:		return new Warrior( id, ETeam.My );
			case 2:		return new Warrior( id, ETeam.Enemy );
			default:
				Console.Error.WriteLine("unknown type: " + type);
				return new Entity( id, EEntityType.Unknown );
		}
	}
	

	public virtual void UpdateParams( string[] inputs )
	{
		NeedDelete = false;
		
		if (PrevPosition.X != int.MinValue)
			PrevPosition = Position.Clone();
		Position.X = int.Parse(inputs[2]);
		Position.Y = int.Parse(inputs[3]);
		if (PrevPosition.X == int.MinValue)
			PrevPosition = Position.Clone();

		Shield = int.Parse(inputs[4]); // Ignore for this league; Count down until shield spell fades
		IsMindControlled = (int.Parse(inputs[5]) == 1); // Ignore for this league; Equals 1 when this entity is under a control spell
	}


	public virtual bool CanDelete()
	{
		return NeedDelete;
	}


	public virtual void Init( string[] inputs )
	{
		IsInitialized = true;
	}


	public override string ToString()
	{
		return $"Type:{Type}, Team:{Team}, Id:{Id}, Pos:{Position}";
	}
}

//################################# Monster #################################//
public enum ESpell
{
	None,
	Wind,
	Control,
	Shield
}

public class Monster: Entity
{
	public bool GoingToBase						= false;
	public Player ThreatFor						= null;
	public float Priority						= 0f;
	public int Health							= 0;
	public int HealthMax						= 0;
	public PointI Velocity						= new PointI();
	public ESpell SpellCast						= ESpell.None;
	public int TurnsToAttack					= -1;
	public int DeadlyTurns						= -1;

	public const int BaseAttackRadius			= 300;
	public const int BaseSeekRadius				= 5000;
	public const int Speed						= 400;


	public Monster( int id ):base( id, EEntityType.Monster )
	{
	}


	public override void UpdateParams( string[] inputs )
	{
		base.UpdateParams( inputs );
		
		Health = int.Parse(inputs[6]); // Remaining health of this monster
		HealthMax = Math.Max( HealthMax, Health );
		Velocity.X = int.Parse(inputs[7]); // Trajectory of this monster
		Velocity.Y = int.Parse(inputs[8]);
		GoingToBase = (int.Parse(inputs[9]) == 1); // 0=monster with no target yet, 1=monster targeting a base
		
		int threatFor = int.Parse(inputs[10]); // Given this monster's trajectory, is it a threat to 1=your base, 2=your opponent's base, 0=neither
		if ( threatFor == 0 )			ThreatFor = null;
		else if ( threatFor == 1 )		ThreatFor = TheGame.PlayerMy;
		else if ( threatFor == 2 )		ThreatFor = TheGame.PlayerEnemy;
		if ( ThreatFor != null )
		{ 
			PointI newPos = new PointI(Position.X + Velocity.X, Position.Y + Velocity.Y);
			if ( PointI.Distance( Position, ThreatFor.BasePosition ) < PointI.Distance( newPos, ThreatFor.BasePosition ) )
				ThreatFor = null;
		}
	}


	public override string ToString()
	{
		return $"MONSTER Id:{Id}, Pos:{Position}, HP:{Health}, Vel:{Velocity}, Shield:{Shield}, Mind:{IsMindControlled} ToBase:{GoingToBase}, Threat:{ThreatFor?.Team}";
	}



	public void CountThreating()
	{
		if ( ThreatFor == null )
			throw new Exception($"not threat {Id}");

		float distToBase = PointI.Distance( ThreatFor.BasePosition, Position, false ) - BaseAttackRadius;
		TurnsToAttack = (int)Math.Ceiling(distToBase / Speed);
	}
}

//################################# Warrior #################################//

public enum EWarriorAction
{
	GoToGuardPosition,
	Practice,
	Attacker,
	DefendBase,
	WindDefence,
}


public class Warrior: Entity
{
	public Monster Target					= null;
	public int WarriorNum					= -1;
	public PointI GuardPoint1				= new PointI();
	public PointI GuardPoint2				= new PointI();
	public PointI AttackPoint1				= new PointI();
	public PointI AttackPoint2				= new PointI();
	private bool GoToPoint1					= true;
	public EWarriorAction Action			= EWarriorAction.GoToGuardPosition;
	public PointI TargetPosition			= new PointI();

	public const int Speed					= 800;
	public const int AttackRadius			= 800;
	public const int AttackDamage			= 2;

	private float guardStayRadius1			= 6000f;
	private float guardStayRadius2			= 9000f;
	private float guardTrainRadius			= 3000f;
	private bool gotoToAttackPos1			= true;


	public Warrior( int id, ETeam team ):base( id, EEntityType.Warrior )
	{
		Team = team;
		if ( team == ETeam.My )
			Belongs = TheGame.PlayerMy;
		else
			Belongs = TheGame.PlayerEnemy;

		Belongs.Warriors.Add( this );
		if ( team == ETeam.My )
			WarriorNum = Belongs.Warriors.Count-1;
	}


	public override void UpdateParams( string[] inputs )
	{
		base.UpdateParams( inputs );
		if ( IsMindControlled )
			Belongs.NeedProtectFromMagic = true;
	}


	public void UpdateGuardPoint()
	{
		float myStartAngle = 0;
		float enemyStartAngle = 0;
		if ( Belongs.BasePosition.X == 0 && Belongs.BasePosition.Y == 0 )
		{
			myStartAngle = 270f;
			enemyStartAngle = 90f;
		}
		else if ( Belongs.BasePosition.X == 17630 && Belongs.BasePosition.Y == 9000 )
		{
			myStartAngle = 90f;
			enemyStartAngle = 270f;
		}
		else
			throw new Exception($"incorrect base pos: {Belongs.BasePosition}");

		float angleSpace = 90f / (Belongs.Warriors.Count + 1);
		float needAngle = myStartAngle + angleSpace * (WarriorNum + 1);

		GuardPoint1 = new PointI(
			Belongs.BasePosition.X + (int)(guardStayRadius1 * Math.Cos(Utils.Radians(needAngle))),
			Belongs.BasePosition.Y - (int)(guardStayRadius1 * Math.Sin(Utils.Radians(needAngle))) );
		GuardPoint2 = new PointI(
			Belongs.BasePosition.X + (int)(guardStayRadius2 * Math.Cos(Utils.Radians(needAngle))),
			Belongs.BasePosition.Y - (int)(guardStayRadius2 * Math.Sin(Utils.Radians(needAngle))) );
		
		AttackPoint1 = new PointI(
			TheGame.PlayerEnemy.BasePosition.X + (int)(5000 * Math.Cos(Utils.Radians(enemyStartAngle + 20f))),
			TheGame.PlayerEnemy.BasePosition.Y - (int)(5000 * Math.Sin(Utils.Radians(enemyStartAngle + 20f))) );
		AttackPoint2 = new PointI(
			TheGame.PlayerEnemy.BasePosition.X + (int)(5000 * Math.Cos(Utils.Radians(enemyStartAngle + 70f))),
			TheGame.PlayerEnemy.BasePosition.Y - (int)(5000 * Math.Sin(Utils.Radians(enemyStartAngle + 70f))) );
	}


	public override void Delete()
	{
		base.Delete();
		Belongs.Warriors.Remove( this );
		Belongs.NeedUpdateGuards = true;
	}


	public override string ToString()
	{
		return $"WARRIOR Team:{Team}, Id:{Id}, Pos:{Position}, Num:{WarriorNum}";
	}


	public String GetCommand()
	{
		if ( CanProtectSelf() )
			return $"SPELL SHIELD {Id}";

		if ( Action == EWarriorAction.Attacker )
			return GetAttackerCommand();

		// defend my base
		if ( Action == EWarriorAction.DefendBase )
		{
			PointI castPos = GetCastControl( Target );
			if ( castPos != null )
				return $"SPELL CONTROL {Target.Id} {castPos.X} {castPos.Y}";

			PointI windPos = GetWindBlowUpPos( Target );
			if ( windPos != null )
				return $"SPELL WIND {windPos.X} {windPos.Y}";
			
			return $"MOVE {TargetPosition.X} {TargetPosition.Y}";
		}
		// defend my base from too close monsters
		else if ( Action == EWarriorAction.WindDefence )
		{
			float distToBase = PointI.Distance( Position, Belongs.BasePosition );
			PointI needPos = Utils.GetEndPointWithOffset( Belongs.BasePosition, Position, 100 );
			return $"SPELL WIND {needPos.X} {needPos.Y}";
		}
		
		// practice
		Target = FindPracticeTarget();		
		if ( Target != null )
		{
			PointI castPos = GetCastControl( Target );
			if ( castPos != null )
				return $"SPELL CONTROL {Target.Id} {castPos.X} {castPos.Y}";
			else
				return $"MOVE {TargetPosition.X} {TargetPosition.Y}";
		}

		// guard
		if ( GoToPoint1 )
		{
			if ( PointI.Distance(GuardPoint1, Position) <= Speed )
				GoToPoint1 = false;
			return $"MOVE {GuardPoint1.X} {GuardPoint1.Y}";
		}
		else
		{
			if ( PointI.Distance(GuardPoint2, Position) <= Speed )
				GoToPoint1 = true;
			return $"MOVE {GuardPoint2.X} {GuardPoint2.Y}";
		}
	}


	
	public String GetAttackerCommand()
	{
		// find nearby monsters
		List<Monster> monsters = TheGame.FindAllByClass<Monster>( EEntityType.Monster );
		for( int i = monsters.Count-1; i >= 0; i-- )
		{
			if ( PointI.Distance( monsters[i].Position, Position ) >= 4400 )
				monsters.RemoveAt( i );
		}
		List<Warrior> enemies = TheGame.PlayerEnemy.Warriors.GetRange(0, TheGame.PlayerEnemy.Warriors.Count);

		// blowup monsters to enemy base
		foreach( var monster in monsters )
		{
			if ( CanHelpBlowUpForMonster( monster, enemies ) )
			{
				PointI blowPos = Utils.GetStartPointWithOffset( monster.Position, TheGame.PlayerEnemy.BasePosition, 2200 );
				return $"SPELL WIND {Position.X + (blowPos.X - monster.Position.X)} {Position.Y + (blowPos.Y - monster.Position.Y)}";
			}
		}
		// blowup enemies
		foreach( var enemy in enemies )
		{
			if ( CanPreventEnemyBlowUp( enemy, monsters ) )
			{
				PointI blowPos = Utils.GetEndPointWithOffset( TheGame.PlayerEnemy.BasePosition, Position, 100 );
				return $"SPELL WIND {blowPos.X} {blowPos.Y}";
			}
		}
		// send monsters to enemy base
		foreach( var monster in monsters )
		{
			PointI castPos = GetCastControl( monster, true );
			if ( castPos != null )
				return $"SPELL CONTROL {monster.Id} {castPos.X} {castPos.Y}";
		}
		// protect monsters
		foreach( var monster in monsters )
		{
			if ( CanProtectMonster( monster ) )
				return $"SPELL SHIELD {monster.Id}";
		}
		// prevent enemies
		foreach( var enemy in enemies )
		{
			if ( CanPreventEnemyControl( enemy, monsters ) )
			{
				PointI controlPos = Utils.GetEndPointWithOffset( TheGame.PlayerEnemy.BasePosition, enemy.Position, 5000 );
				return $"SPELL CONTROL {enemy.Id} {controlPos.X} {controlPos.Y}";
			}
		}

		PointI escort = EscortMonster( monsters, enemies );
		if ( escort != null )
			return $"MOVE {escort.X} {escort.Y}";

		// guard
		if ( gotoToAttackPos1 )
		{
			if ( PointI.Distance( Position, AttackPoint1 ) <= Speed )
				gotoToAttackPos1 = false;
			return $"MOVE {AttackPoint1.X} {AttackPoint1.Y}";
		}
		else
		{
			if ( PointI.Distance( Position, AttackPoint2 ) <= Speed )
				gotoToAttackPos1 = true;
			return $"MOVE {AttackPoint2.X} {AttackPoint2.Y}";
		}
	}


	private PointI EscortMonster( List<Monster> monsters, List<Warrior> enemies )
	{
		bool haveWithoutShield = false;
		foreach( var monster in monsters )
		{
			if ( monster.Shield <= 0 )
				haveWithoutShield = true;
		}
		foreach( var enemy in enemies )
		{
			if ( enemy.Shield <= 3 )
				haveWithoutShield = true;
		}
		if ( !haveWithoutShield )
			return null;

		Monster bestMonster = null;
		float bestDist = 0;
		foreach( var monster in monsters )
		{
			if ( monster.ThreatFor != TheGame.PlayerEnemy || PointI.Distance( monster.Position, TheGame.PlayerEnemy.BasePosition ) > 5000 )
				continue;
			float dist = PointI.Distance( monster.Position, Position );
			if ( bestMonster == null || bestDist > dist )
			{
				bestMonster = monster;
				bestDist = dist;
			}
		}

		if ( bestMonster != null )
			return Utils.GetEndPointWithOffset( TheGame.PlayerEnemy.BasePosition, bestMonster.Position, Warrior.AttackRadius + 100 );

		return null;
	}


	private bool CanPreventEnemyBlowUp( Warrior enemy, List<Monster> monsters )
	{
		if ( Belongs.Mana <= 100
			|| enemy.Shield > 0
			|| PointI.Distance( enemy.Position, Position ) > 1280
			|| PointI.Distance( enemy.Position, TheGame.PlayerEnemy.BasePosition ) > 5000 )
			return false;

		// monsters nearby
		foreach( var monster in monsters )
		{
			if ( monster.ThreatFor == TheGame.PlayerEnemy && PointI.Distance( Position, monster.Position ) <= 1280 )
				return false;
		}
		return EnemyHaveTarget( enemy, monsters );
	}


	private bool CanPreventEnemyControl( Warrior enemy, List<Monster> monsters )
	{
		if ( Belongs.Mana <= 100
			|| enemy.Shield > 0
			|| PointI.Distance( enemy.Position, Position ) > 2200
			|| PointI.Distance( enemy.Position, TheGame.PlayerEnemy.BasePosition ) > 5000 )
			return false;
		
		return EnemyHaveTarget( enemy, monsters );
	}


	private bool EnemyHaveTarget( Warrior enemy, List<Monster> monsters )
	{
		foreach( var monster in monsters )
		{
			if ( monster.ThreatFor == TheGame.PlayerEnemy && PointI.Distance( enemy.Position, monster.Position ) <= Warrior.AttackRadius * 2 )
				return true;
		}
		return false;
	}


	private bool CanHelpBlowUpForMonster( Monster monster, List<Warrior> enemies )
	{
		if ( Belongs.Mana <= 100
			|| monster.Shield > 0
			|| PointI.Distance( monster.Position, Position ) > 1280
			|| PointI.Distance( monster.Position, TheGame.PlayerEnemy.BasePosition ) > 7200 )
			return false;
		
		// enemies nearby
		foreach( var enemy in enemies )
		{
			if ( PointI.Distance( enemy.Position, Position ) <= 1280 )
				return false;
		}
		return true;
	}


	private bool CanProtectMonster( Monster monster )
	{
		if ( Belongs.Mana <= 100
			|| monster.Shield > 0
			|| PointI.Distance( monster.Position, Position ) > 2200
			|| monster.Health < monster.HealthMax * 0.5f
			|| PointI.Distance( monster.Position, TheGame.PlayerEnemy.BasePosition ) > 5100
			|| monster.ThreatFor != TheGame.PlayerEnemy )
			return false;

		return true;
	}


	public bool CanProtectSelf()
	{
		if ( Belongs.Mana <= ((Action == EWarriorAction.Attacker) ? 150 : 50)
			|| Shield > 0
			|| !Belongs.NeedProtectFromMagic )
			return false;

		float minDistToEnemyWarrior = 100000;
		foreach( var enemy in TheGame.PlayerEnemy.Warriors )
		{
			float dist = PointI.Distance( Position, enemy.Position );
			if ( minDistToEnemyWarrior > dist )
				minDistToEnemyWarrior = dist;
		}

		return (minDistToEnemyWarrior <= 2200 + Warrior.Speed * 2);
	}


	public Monster FindPracticeTarget()
	{
		List<Monster> monsters = Belongs.PracticeMonsters.GetRange( 0, Belongs.PracticeMonsters.Count );
		if ( monsters.Count == 0 )
			return null;
		
		foreach( var monster in monsters )
		{
			monster.Priority = -1f;
			float monsterToGuard = PointI.Distance( GoToPoint1 ? GuardPoint1 : GuardPoint2, monster.Position, false );
			float monsterToBase = PointI.Distance( Belongs.BasePosition, monster.Position, false );
			if ( monsterToGuard <= guardTrainRadius )
			{
				float maxDist = guardTrainRadius + (GoToPoint1 ? guardStayRadius1 : guardStayRadius2);
				int numNeigb = 0;
				GetOptimalAttackPosition( monster, monsters, out numNeigb );
				monster.Priority = numNeigb * 100f + (guardTrainRadius - monsterToGuard) / guardTrainRadius * 100f; // близко к охранной точке prior+600, далеко prior+500
			}
		}

		monsters.Sort( (m1, m2) => (int)(m2.Priority - m1.Priority) );
		if ( monsters[0].Priority >= 0f )
		{
			int temp = 0;
			TargetPosition = GetOptimalAttackPosition( monsters[0], monsters, out temp );
			return monsters[0];
		}
		else
			return null;
	}


	public PointI GetOptimalAttackPosition( Monster target, List<Monster> otherMonsters, out int numTargets )
	{
		PointI optimalPnt = new PointI();
		numTargets = 0;

		// find minimal circle with maximum monsters inside
		// have only target monster
		if ( otherMonsters.Count == 1 )
		{
			numTargets = 1;
			optimalPnt.X = target.Position.X;
			optimalPnt.Y = target.Position.Y;
		}
		// find second monster with maximum neighbours
		else
		{
			List<KeyValuePair<Monster, int>> monstersNearby = new List<KeyValuePair<Monster, int>>();
			foreach( var neibr in otherMonsters )
			{
				if ( neibr == target )
					continue;
				float dist = PointI.Distance( neibr.Position, target.Position );
				if ( dist > Warrior.AttackRadius*2 - 6 )
					continue;

				PointI midPoint = new PointI(
					target.Position.X + (neibr.Position.X - target.Position.X) / 2,
					target.Position.Y + (neibr.Position.Y - target.Position.Y) / 2 );
				int totalNeigbours = 0;
				foreach( var neibr2 in otherMonsters )
				{
					if ( neibr2 == neibr || neibr2 == target )
						continue;
					if ( PointI.Distance( neibr2.Position, midPoint ) <= Warrior.AttackRadius-3 )
						totalNeigbours++;
				}
				monstersNearby.Add( new KeyValuePair<Monster, int>(neibr, totalNeigbours) );
			}

			monstersNearby.Sort( (n1, n2) => { return n2.Value - n1.Value; } );// most farest is first
			// have only target monster
			if ( monstersNearby.Count == 0 )
			{
				numTargets = 1;
				optimalPnt.X = target.Position.X;
				optimalPnt.Y = target.Position.Y;
			}
			else
			{
				numTargets = 2 + monstersNearby[0].Value;
				optimalPnt.X = target.Position.X + (monstersNearby[0].Key.Position.X - target.Position.X) / 2;
				optimalPnt.Y = target.Position.Y + (monstersNearby[0].Key.Position.Y - target.Position.Y) / 2;
			}
		}

		return optimalPnt;
	}


	public PointI GetWindBlowUpPos( Monster target )
	{
		if ( Belongs.Mana <= 100 )
			return null;

		if ( PointI.Distance( target.Position, Position ) > 1280
			|| target.Shield > 0
			|| target.SpellCast != ESpell.None
			|| PointI.Distance( target.Position, Belongs.BasePosition ) > Monster.BaseSeekRadius )
			return null;

		PointI castPos = Utils.GetEndPointWithOffset( Belongs.BasePosition, Position, 100f );
		PointI windVector = new PointI( target.Position.X + (castPos.X - Position.X), target.Position.Y + (castPos.Y - Position.Y) );
		PointI newTargPos = Utils.GetStartPointWithOffset( target.Position, windVector, 2200 );
		if ( PointI.Distance( newTargPos, Belongs.BasePosition ) > Monster.BaseSeekRadius )
		{
			target.SpellCast = ESpell.Wind;
			return castPos;
		}
		else
			return null;
	}


	public PointI GetCastControl( Monster target, bool forAttacker = false )
	{
		if ( !forAttacker )
			return null;

		if ( Belongs.Mana <= 100
			|| TheGame.Turn < 100
			|| PointI.Distance( target.Position, Position ) > 2200
			|| target.Health < target.HealthMax * 0.75f
			|| target.Shield > 0
			|| target.SpellCast != ESpell.None
			|| PointI.Distance( target.Position, Belongs.BasePosition ) <= Monster.BaseSeekRadius
			|| target.ThreatFor == TheGame.PlayerEnemy )
			return null;
		
		float startAngle = 0f;
		if ( Game.This.PlayerEnemy.BasePosition.X == 0 && Game.This.PlayerEnemy.BasePosition.Y == 0 )
			startAngle = 270f;
		else if ( Game.This.PlayerEnemy.BasePosition.X == 17630 && Game.This.PlayerEnemy.BasePosition.Y == 9000 )
			startAngle = 90f;

		float needAngle = startAngle + Belongs.AttackAngle;
		PointI castPos = new PointI(
			Game.This.PlayerEnemy.BasePosition.X + (int)(4950 * Math.Cos(Utils.Radians(needAngle))),
			Game.This.PlayerEnemy.BasePosition.Y - (int)(4950 * Math.Sin(Utils.Radians(needAngle))) );

		if ( forAttacker )
			castPos = Game.This.PlayerEnemy.BasePosition.Clone();

		Belongs.AttackAngle += 10;
		if ( Belongs.AttackAngle >= 90 )
			Belongs.AttackAngle = 10;
		target.SpellCast = ESpell.Control;
		return castPos;
	}
}

//################################# SIMULATION #################################//

public class WarriorState
{
	public Warrior Host								= null;
	public PointI PositionStart						= new PointI();
	public PointI PositionEnd						= new PointI();
	public PointI PositionTarget					= new PointI();
	public Monster MainTarget						= null;
	public MonsterState MainTargetState				= null;
	public List<MonsterState> SecondaryTargets		= new List<MonsterState>();


	public WarriorState GetNextTurn()
	{
		WarriorState clone = new WarriorState();
		clone.Host = Host;
		clone.PositionStart = PositionEnd.Clone();
		clone.MainTarget = MainTarget;
		return clone;
	}

	public override string ToString()
	{
		return $"{Host.Id}:{MainTarget.Id}{PositionStart}-{PositionTarget}";
	}
}


public class MonsterState
{
	public Monster Host								= null;
	public PointI PositionStart						= new PointI();
	public PointI PositionEnd						= new PointI();
	public int HealthStart							= 0;
	public int HealthEnd							= 0;
	public PointI Velocity							= new PointI();
	public List<WarriorState> Attackers				= new List<WarriorState>();
	public bool GoingToBase							= false;
	public bool BaseReached							= false;


	public MonsterState GetNextTurn()
	{
		if ( BaseReached || HealthStart <= 0 )
			return null;

		MonsterState clone = new MonsterState();
		clone.Host = Host;
		clone.PositionStart = PositionEnd.Clone();
		clone.HealthStart = HealthEnd;
		clone.GoingToBase = GoingToBase;
		clone.Velocity = Velocity.Clone();

		PointI basePos = Game.This.PlayerMy.BasePosition;

		// change velocity if monster come close to base 
		if ( !clone.GoingToBase )
		{
			float distToBase = PointI.Distance( clone.PositionStart, basePos );
			if ( distToBase <= Monster.BaseSeekRadius )
			{
				clone.GoingToBase = true;
				clone.Velocity.X = (int)( Monster.Speed * (basePos.X - clone.PositionStart.X) / distToBase );
				clone.Velocity.Y = (int)( Monster.Speed * (basePos.Y - clone.PositionStart.Y) / distToBase );
			}
		}

		// monster came to base
		if ( !clone.BaseReached )
		{
			float distToBase = PointI.Distance( clone.PositionStart, basePos );
			if ( distToBase <= Monster.BaseAttackRadius )
				clone.BaseReached = true;
		}

		return clone;
	}


	public void Simulate()
	{
		HealthEnd = HealthStart;
		if ( !BaseReached && HealthStart > 0 )
		{			
			PositionEnd.X = PositionStart.X + Velocity.X;
			PositionEnd.Y = PositionStart.Y + Velocity.Y;
		}
	}


	public override string ToString()
	{
		//return $"{Host.Id}:{(GoingToBase ? "Y" : "N")}{(BaseReached ? "Y" : "N")}{PositionStart}({HealthStart})";
		return $"{Host.Id}:{PositionStart}-{HealthStart}";
	}
}


public class TurnState
{
	public List<MonsterState> Monsters				= new List<MonsterState>();
	public List<WarriorState> Warriors				= new List<WarriorState>();
	public int ManaStart							= 0;
	public int ManaEnd								= 0;


	public TurnState GetNextMonsterTurn()
	{
		TurnState clone = new TurnState();
		clone.ManaStart = ManaEnd;

		foreach( var monstr in Monsters )
		{
			MonsterState nextMstr = monstr.GetNextTurn();
			if ( nextMstr != null )
				clone.Monsters.Add( nextMstr );
		}

		clone.SimulateMonsters();

		if ( clone.Monsters.Count == 0 )
			return null;

		return clone;
	}


	public MonsterState FindMonsterState( Monster monster )
	{
		foreach( var state in Monsters )
		{
			if ( state.Host == monster )
				return state;
		}
		return null;
	}


	public WarriorState SimulateWarrior( WarriorState warrior, MonsterState target )
	{
		Warriors.Add( warrior );
		warrior.MainTargetState = target;
		
		// find position to come
		warrior.PositionTarget = GetOptimalAttackPosition( warrior, target );
		float distToTarg = PointI.Distance( warrior.PositionTarget, warrior.PositionStart );
		if ( distToTarg <= Warrior.Speed )
			warrior.PositionEnd = warrior.PositionTarget.Clone();
		else
			warrior.PositionEnd = Utils.GetStartPointWithOffset( warrior.PositionStart, warrior.PositionTarget, Warrior.Speed );

		// substract monsters health and gain mana
		foreach( var monster in Monsters )
		{
			if ( PointI.Distance( monster.PositionStart, warrior.PositionEnd ) <= Warrior.AttackRadius )
			{
				monster.HealthEnd -= 2;
				ManaEnd += 2;
				warrior.SecondaryTargets.Add( monster );
			}
		}

		WarriorState newState = new WarriorState();
		newState.MainTarget = warrior.MainTarget;
		newState.Host = warrior.Host;
		newState.PositionStart = warrior.PositionEnd.Clone();
		return newState;
	}


	public PointI GetOptimalAttackPosition( WarriorState warrior, MonsterState target )
	{
		PointI optimalPnt = new PointI();

		// find minimal circle with maximum monsters inside
		// have only target monster
		if ( Monsters.Count == 1 )
		{
			optimalPnt.X = target.PositionStart.X;
			optimalPnt.Y = target.PositionStart.Y;
		}
		// find second monster with maximum neighbours
		else
		{
			List<KeyValuePair<MonsterState, int>> monstersNearby = new List<KeyValuePair<MonsterState, int>>();
			foreach( var neibr in Monsters )
			{
				if ( neibr == target )
					continue;
				float dist = PointI.Distance( neibr.PositionStart, target.PositionStart );
				if ( dist > Warrior.AttackRadius*2 - 6 )
					continue;

				PointI midPoint = new PointI(
					target.PositionStart.X + (neibr.PositionStart.X - target.PositionStart.X) / 2,
					target.PositionStart.Y + (neibr.PositionStart.Y - target.PositionStart.Y) / 2 );
				int totalNeigbours = 0;
				foreach( var neibr2 in Monsters )
				{
					if ( neibr2 == neibr || neibr2 == target )
						continue;
					if ( PointI.Distance( neibr2.PositionStart, midPoint ) <= Warrior.AttackRadius-3 )
						totalNeigbours++;
				}
				monstersNearby.Add( new KeyValuePair<MonsterState, int>(neibr, totalNeigbours) );
				//Console.Error.WriteLine($"found targ{target.Host.Id} neib{neibr.Host.Id} - {totalNeigbours}");//zzz
			}

			monstersNearby.Sort( (n1, n2) => { return n2.Value - n1.Value; } );// most farest is first
			// have only target monster
			if ( monstersNearby.Count == 0 )
			{
				optimalPnt.X = target.PositionStart.X;
				optimalPnt.Y = target.PositionStart.Y;
			}
			else
			{
				optimalPnt.X = target.PositionStart.X + (monstersNearby[0].Key.PositionStart.X - target.PositionStart.X) / 2;
				optimalPnt.Y = target.PositionStart.Y + (monstersNearby[0].Key.PositionStart.Y - target.PositionStart.Y) / 2;
			}
		}

		// offset optimal point away from base
		float minRadius = PointI.Distance( optimalPnt, target.PositionStart );
		float offset = Warrior.AttackRadius - minRadius - 2;
		if ( offset > 0 )
		{
			PointI basePos = Game.This.PlayerMy.BasePosition;
			float baseDist = PointI.Distance( basePos, optimalPnt );
			optimalPnt.X = (int)(optimalPnt.X + (optimalPnt.X - basePos.X) / baseDist * offset);
			optimalPnt.Y = (int)(optimalPnt.Y + (optimalPnt.Y - basePos.Y) / baseDist * offset);
		}

		return optimalPnt;
	}


	public void UpdateMonsterSimulation( TurnState prevTurn )
	{
		for( int i = Monsters.Count - 1; i >= 0; i-- )
		{
			MonsterState currMonstr = Monsters[i];
			MonsterState prevMonstr = prevTurn.FindMonsterState( currMonstr.Host );
			if ( prevMonstr == null || prevMonstr.HealthStart <= 0 )
				Monsters.RemoveAt( i );
			else
			{
				currMonstr.HealthStart = prevMonstr.HealthEnd;
				currMonstr.HealthEnd = prevMonstr.HealthEnd;
				if ( currMonstr.HealthStart <= 0 )
					currMonstr.BaseReached = false;
			}
		}
	}


	public void SimulateMonsters()
	{
		ManaEnd = ManaStart;
		foreach( var monstr in Monsters )
			monstr.Simulate();
	}


	public override string ToString()
	{
		String turn = "";
		foreach( var mstr in Monsters )
			turn += " " + mstr.ToString();
		//turn += " | ";
		//foreach( var war in Warriors )
		//	turn += " " + war.ToString();
		return turn;
	}
}


public class Simulation
{
	public List<TurnState> Turns					= new List<TurnState>();


	public void SimulateMonsterWalk( List<Monster> monsters )
	{
		Turns.Clear();
		TurnState lastState = new TurnState();
		lastState.ManaStart = Game.This.PlayerMy.Mana;
		Turns.Add( lastState );

		foreach( var monstr in monsters )
		{
			MonsterState monstrSt = new MonsterState();
			monstrSt.PositionStart = monstr.Position.Clone();
			monstrSt.Host = monstr;
			monstrSt.HealthStart = monstr.Health;
			monstrSt.Velocity = monstr.Velocity.Clone();
			monstrSt.GoingToBase = monstr.GoingToBase;
			lastState.Monsters.Add( monstrSt );
		}

		lastState.SimulateMonsters();
		while( lastState != null )
		{
			lastState = lastState.GetNextMonsterTurn();
			if ( lastState != null )
				Turns.Add( lastState );
		}
	}


	public void SetAllDeadlyMark()
	{
		for( int i = 0; i < Turns.Count; i++ )
		{
			foreach( var monster in Turns[i].Monsters )
			{
				if ( monster.BaseReached && monster.Host.DeadlyTurns < 0 )
					monster.Host.DeadlyTurns = i;
			}
		}
	}


	public Monster GetPriorityTarget()
	{
		foreach( var turn in Turns )
		{
			foreach( var monster in turn.Monsters )
			{
				if ( monster.BaseReached )
					return monster.Host;
			}
		}

		return null;
	}


	public WarriorState AddAttacker( Warrior warrior, Monster target )
	{
		WarriorState warriorSt = new WarriorState();
		warriorSt.Host = warrior;
		warriorSt.MainTarget = target;
		warriorSt.PositionStart = warrior.Position.Clone();

		WarriorState lastState = warriorSt;
		TurnState prevTurn = null;
		for( int i = 0; i < Turns.Count; i++ )
		{
			var turn = Turns[i];
			if ( i > 0 )
			{
				for( int j = i; j < Turns.Count; j++ )
					Turns[j].UpdateMonsterSimulation( Turns[j-1] );
			}

			MonsterState targetState = turn.FindMonsterState( target );
			if ( targetState == null || targetState.HealthStart == 0 )
			{
				target = GetPriorityTarget();
				if ( target == null )
					break;
				else
					targetState = turn.FindMonsterState( target );
				if ( targetState == null )
					break;
			}
			lastState = turn.SimulateWarrior( lastState, targetState );
			prevTurn = turn;
		}

		return warriorSt;
	}


	public override string ToString()
	{
		String sim = "";
		for( int i = 0; i < Turns.Count; i++ )
			sim += $"{i}:{Turns[i].ToString()}\n";
		return sim;
	}
}

//################################# UTILS #################################//

public class PointI
{
	public int X = 0;
	public int Y = 0;

	public PointI( int x = 0, int y = 0 )
	{
		X = x;
		Y = y;
	}

	public PointI Clone()
	{
		return new PointI( X, Y );
	}

	public static float Distance( PointI pnt1, PointI pnt2, bool manhatan = false )
	{
		if (manhatan)
			return Math.Abs(pnt1.X - pnt2.X) + Math.Abs(pnt1.Y - pnt2.Y);
		else
			return (float)Math.Sqrt((pnt1.X - pnt2.X) * (pnt1.X - pnt2.X) + (pnt1.Y - pnt2.Y) * (pnt1.Y - pnt2.Y));
	}

	public override String ToString()
	{
		return String.Format("({0},{1})", X, Y);
	}
	
	public bool Equals(PointI pnt)
	{
		return X == pnt.X && Y == pnt.Y;
	}

	/// <summary>
	/// in radians (6.28)
	/// </summary>
	public static float GetAngle( PointI firstPnt, PointI secondPnt )
	{
		float width = -(firstPnt.X - secondPnt.X);
		float height = (firstPnt.Y - secondPnt.Y);
		float length = (float)Math.Sqrt( width * width + height * height );
		if ( length == 0f )
			return 0f;
		float angle = (float)Math.Asin( height/length );
		if ( width < 0 ) 
			angle = (float)(Math.PI - angle);
		return angle;
	}
}

public class PointF
{
	public float X = 0;
	public float Y = 0;

	public PointF( float x = 0, float y = 0 )
	{
		X = x;
		Y = y;
	}

	public PointF Clone()
	{
		return new PointF( X, Y );
	}

	public static float Distance( PointF pnt1, PointF pnt2 )
	{
		return (float)Math.Sqrt((pnt1.X - pnt2.X) * (pnt1.X - pnt2.X) + (pnt1.Y - pnt2.Y) * (pnt1.Y - pnt2.Y));
	}

	public override String ToString()
	{
		return String.Format("({0:0.0},{1:0.0})", X, Y);
	}
	
	public bool Equals(PointF pnt)
	{
		return X == pnt.X && Y == pnt.Y;
	}

	/// <summary>
	/// in radians (6.28)
	/// </summary>
	public static float GetAngle( PointF firstPnt, PointF secondPnt )
	{
		float width = -(firstPnt.X - secondPnt.X);
		float height = (firstPnt.Y - secondPnt.Y);
		float length = (float)Math.Sqrt( width * width + height * height );
		if ( length == 0f )
			return 0f;
		float angle = (float)Math.Asin( height/length );
		if ( width < 0 ) 
			angle = (float)(Math.PI - angle);
		if ( angle >= Math.PI*2 )
			angle -= (float)Math.PI*2;
		if ( angle < 0 )
			angle += (float)Math.PI*2;
		return angle;
	}
}

public class Utils
{
	public static float InvPI = (float)(180d / Math.PI);
	private static Random rnd = null;
	
	public static PointI GetEndPointWithOffset( PointI startPnt, PointI endPnt, float offsetFromEnd )
	{
		float dist = PointI.Distance( startPnt, endPnt );
		return new PointI(
				endPnt.X + (int)((endPnt.X - startPnt.X) / dist * offsetFromEnd),
				endPnt.Y + (int)((endPnt.Y - startPnt.Y) / dist * offsetFromEnd) );
	}


	public static PointI GetStartPointWithOffset( PointI startPnt, PointI endPnt, float offsetFromStart )
	{
		float dist = PointI.Distance( startPnt, endPnt );
		return new PointI(
				startPnt.X + (int)((endPnt.X - startPnt.X) / dist * offsetFromStart),
				startPnt.Y + (int)((endPnt.Y - startPnt.Y) / dist * offsetFromStart) );
	}

	
	public static String DirToString(EDir dir)
	{
		switch (dir)
		{
			case EDir.Down: return "DOWN";
			case EDir.Up: return "UP";
			case EDir.Left: return "LEFT";
			case EDir.Right: return "RIGHT";
		}
		throw new Exception("unknown");
	}


	public static PointI DirToOffset(EDir dir)
	{
		switch (dir)
		{
			case EDir.Down: return new PointI(0, 1);
			case EDir.Up: return new PointI(0, -1);
			case EDir.Left: return new PointI(-1, 0);
			case EDir.Right: return new PointI(1, 0);
		}
		throw new Exception("unknown");
	}
	

	public static EDir DirToOposite(EDir dir)
	{
		switch (dir)
		{
			case EDir.Down: return EDir.Up;
			case EDir.Up: return EDir.Down;
			case EDir.Left: return EDir.Right;
			case EDir.Right: return EDir.Left;
		}
		throw new Exception("unknown");
	}
	

	/// <summary>
	/// 180 deg -> 3.14 rad
	/// </summary>
	public static float Radians( float deg )
	{
		return deg / InvPI;
	}


	/// <summary>
	/// 3.14 rad -> 180 deg
	/// </summary>
	public static float Degrees( float rad )
	{
		return rad * InvPI;
	}

	/// crop value f
	public static int Clamp( int f, int min, int max )
	{
		return (f < min) ? min : ( (f > max) ? max : f );
	}


	/// crop value f
	public static float Clamp( float f, float min, float max )
	{
		return (f < min) ? min : ( (f > max) ? max : f );
	}

	public static int RandomI( int min, int max )
	{
		if ( rnd == null )
			rnd = new Random( 0 );
		return rnd.Next( min, max );
	}

	public static int RandomI( int max )
	{
		if ( rnd == null )
			rnd = new Random( 0 );
		return rnd.Next( max );
	}

	public static float RandomF( float min, float max )
	{
		if ( rnd == null )
			rnd = new Random( 0 );
		return min + ((float)rnd.Next( 1000000 ) / 1000000f) * (max - min);
	}

	public static float RandomF( float max )
	{
		if ( rnd == null )
			rnd = new Random( 0 );
		return ((float)rnd.Next( 1000000 ) / 1000000f) * max;
	}
}
