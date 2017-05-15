using Box2DX.Dynamics;
using Gale.Props;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale
{
	class TriggerCallback : ContactListener
	{
		public Action<Prop> EventCallback { get; private set; }
		public bool Tripped { get; private set; } = false;

		public TriggerCallback(Action<Prop> event_callback)
			=> EventCallback = event_callback;
		public override void Add(ContactPoint contact)
		{
			Prop sensor;
			Prop trigger;
			if (!Tripped && TryGetSensorTrip(contact, out sensor, out trigger))
			{
				EventCallback(sensor);
				Tripped = true;
			}
		}

		public override void Remove(ContactPoint contact)
		{
			Prop sensor;
			Prop trigger;
			if (Tripped && TryGetSensorTrip(contact, out sensor, out trigger))
			{
				Tripped = false;
			}
		}
		static bool TryGetSensorTrip(ContactPoint contact, out Prop sensor, out Prop tripped_by)
		{
			sensor = null;
			tripped_by = null;
			var fix_a = contact.Shape1;
			var fix_b = contact.Shape2;

			//make sure only one of the fixtures was a sensor
			bool sensorA = fix_a.IsSensor;
			bool sensorB = fix_b.IsSensor;
			if (!(sensorA ^ sensorB))
				return false;

			var entityA = (Prop)(fix_a.GetBody().GetUserData());
			var entityB = (Prop)(fix_b.GetBody().GetUserData());

			if (sensorA)
			{
				sensor = entityA;
				tripped_by = entityB;
			}
			else
			{
				sensor = entityB;
				tripped_by = entityA;
			}
			return true;
		}

	}
}
