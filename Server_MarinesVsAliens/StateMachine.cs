using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_MarinesVsAliens
{
    public class StateMachine
    {
        
        private Room room;
        private Room.RoomState currentState;
        public Room.RoomState CurrentState
        {
            get
            {
                return currentState;
            }
        }

        
        public StateMachine(Room room)            
        {
            this.room = room;
            currentState = Room.RoomState.LOBBY;
           
        }

        public void Update()
        {
            room.Update();
        }

        private void OnChangeState(Room.RoomState state)
        {
            switch (state)
            {
                case Room.RoomState.LOBBY:
                    break;
                case Room.RoomState.GAME:
                    break;                
            }
        }

        public void ChangeState(Room.RoomState state)
        {
            OnChangeState(state);
            currentState = state;
        }

    }
}
