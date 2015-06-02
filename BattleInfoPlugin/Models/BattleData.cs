﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using BattleInfoPlugin.Models.Raw;
using BattleInfoPlugin.Models.Repositories;
using Grabacr07.KanColleWrapper;
using Grabacr07.KanColleWrapper.Models;
using Livet;

namespace BattleInfoPlugin.Models
{
    public class BattleData : NotificationObject
    {
        //FIXME 敵の開幕雷撃&連合艦隊がまだ不明(とりあえず第二艦隊が受けるようにしてる)
        
        #region Name変更通知プロパティ
        private string _Name;

        public string Name
        {
            get
            { return this._Name; }
            set
            { 
                if (this._Name == value)
                    return;
                this._Name = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region UpdatedTime変更通知プロパティ
        private DateTimeOffset _UpdatedTime;

        public DateTimeOffset UpdatedTime
        {
            get
            { return this._UpdatedTime; }
            set
            { 
                if (this._UpdatedTime == value)
                    return;
                this._UpdatedTime = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region BattleSituation変更通知プロパティ

        private BattleSituation _BattleSituation;

        public BattleSituation BattleSituation
        {
            get
            { return this._BattleSituation; }
            set
            {
                if (this._BattleSituation == value)
                    return;
                this._BattleSituation = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region FirstFleet変更通知プロパティ
        private FleetData _FirstFleet;

        public FleetData FirstFleet
        {
            get
            { return this._FirstFleet; }
            set
            { 
                if (this._FirstFleet == value)
                    return;
                this._FirstFleet = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region SecondFleet変更通知プロパティ
        private FleetData _SecondFleet;

        public FleetData SecondFleet
        {
            get
            { return this._SecondFleet; }
            set
            { 
                if (this._SecondFleet == value)
                    return;
                this._SecondFleet = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region Enemies変更通知プロパティ
        private FleetData _Enemies;

        public FleetData Enemies
        {
            get
            { return this._Enemies; }
            set
            { 
                if (this._Enemies == value)
                    return;
                this._Enemies = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region FriendAirSupremacy変更通知プロパティ
        private AirSupremacy _FriendAirSupremacy = AirSupremacy.航空戦なし;

        public AirSupremacy FriendAirSupremacy
        {
            get
            { return this._FriendAirSupremacy; }
            set
            { 
                if (this._FriendAirSupremacy == value)
                    return;
                this._FriendAirSupremacy = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        #region RankPrediction Change Notify
        private BattleRank? _RankPrediction = null;

        public BattleRank? RankPrediction
        {
            get
            { return this._RankPrediction; }
            set
            {
                if (this._RankPrediction == value)
                    return;
                this._RankPrediction = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        private int CurrentDeckId { get; set; }

        private readonly EnemyDataProvider provider = new EnemyDataProvider();

        public BattleData()
        {
            var proxy = KanColleClient.Current.Proxy;

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_battle_midnight/battle")
                .TryParse<battle_midnight_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_battle_midnight/sp_midnight")
                .TryParse<battle_midnight_sp_midnight>().Subscribe(x => this.Update(x.Data));

            proxy.api_req_combined_battle_airbattle
                .TryParse<combined_battle_airbattle>().Subscribe(x => this.Update(x.Data));

            proxy.api_req_combined_battle_battle
                .TryParse<combined_battle_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_combined_battle/battle_water")
                .TryParse<combined_battle_battle_water>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_combined_battle/midnight_battle")
                .TryParse<combined_battle_midnight_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_combined_battle/sp_midnight")
                .TryParse<combined_battle_sp_midnight>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_practice/battle")
                .TryParse<practice_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_practice/midnight_battle")
                .TryParse<practice_midnight_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_sortie/airbattle")
                .TryParse<sortie_airbattle>().Subscribe(x => this.Update(x.Data));

            proxy.api_req_sortie_battle
                .TryParse<sortie_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_map/start")
                .TryParse<map_start_next>().Subscribe(x => this.UpdateFleetsByStartNext(x.Data, x.Request["api_deck_id"]));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_map/next")
                .TryParse<map_start_next>().Subscribe(x => this.UpdateFleetsByStartNext(x.Data));

            proxy.api_req_sortie_battleresult
                .TryParse<battle_result>().Subscribe(x => this.Update(x.Data));

            proxy.api_req_combined_battle_battleresult
                .TryParse<battle_result>().Subscribe(x => this.Update(x.Data));

        }

        public Dictionary<MapInfo, Dictionary<MapCell, Dictionary<int, FleetData>>> GetMapEnemies()
        {
            return this.provider.GetMapEnemies();
        }

        public Dictionary<MapCell, CellType> GetCellTypes()
        {
            var cells = Repositories.Master.Current.MapCells.Select(c => c.Value);
            return this.provider.GetMapCellBattleTypes()
                .SelectMany(x => x.Value, (x, y) => new { cell = cells.Single(c => c.MapInfoId == x.Key && c.IdInEachMapInfo == y.Key), type = y.Value })
                .Select(x => new { x.cell, type = x.type.ToCellType() | x.cell.ColorNo.ToCellType() })
                .ToDictionary(x => x.cell, x => x.type);
        }

        #region Update From Battle SvData

        public void Update(battle_midnight_battle data)
        {
            this.Name = "通常 - 夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.PreUpdateBattleRank();

            this.FirstFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());

            this.UpdateBattleRank();
        }

        public void Update(battle_midnight_sp_midnight data)
        {
            this.Name = "通常 - 開幕夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation, data.api_eSlot);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.PreUpdateBattleRank();

            this.FirstFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());

            this.UpdateBattleRank();

            this.FriendAirSupremacy = AirSupremacy.航空戦なし;

            this.provider.UpdateBattleTypes(data);
        }

        public void Update(combined_battle_airbattle data)
        {
            this.Name = "連合艦隊 - 航空戦 - 昼戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation, data.api_eSlot);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.PreUpdateBattleRank(true);

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_kouku2.GetFirstFleetDamages()
                );

            this.SecondFleet.CalcDamages(
                data.api_kouku.GetSecondFleetDamages(),
                data.api_kouku2.GetSecondFleetDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),
                data.api_kouku.GetEnemyDamages(),
                data.api_kouku2.GetEnemyDamages()
                );

            this.UpdateBattleRank(true);

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy(); //航空戦2回目はスルー

            this.provider.UpdateBattleTypes(data);
        }

        public void Update(combined_battle_battle data)
        {
            this.Name = "連合艦隊 - 機動部隊 - 昼戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation, data.api_eSlot);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.PreUpdateBattleRank(true);

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_hougeki2.GetFriendDamages(),
                data.api_hougeki3.GetFriendDamages()
                );

            this.SecondFleet.CalcDamages(
                data.api_kouku.GetSecondFleetDamages(),
                data.api_opening_atack.GetFriendDamages(),
                data.api_hougeki1.GetFriendDamages(),
                data.api_raigeki.GetFriendDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),
                data.api_kouku.GetEnemyDamages(),
                data.api_opening_atack.GetEnemyDamages(),
                data.api_hougeki1.GetEnemyDamages(),
                data.api_raigeki.GetEnemyDamages(),
                data.api_hougeki2.GetEnemyDamages(),
                data.api_hougeki3.GetEnemyDamages()
                );

            this.UpdateBattleRank(true);

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy();

            this.provider.UpdateBattleTypes(data);
        }

        public void Update(combined_battle_battle_water data)
        {
            this.Name = "連合艦隊 - 水上部隊 - 昼戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation, data.api_eSlot);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.PreUpdateBattleRank(true);

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_hougeki1.GetFriendDamages(),
                data.api_hougeki2.GetFriendDamages()
                );

            this.SecondFleet.CalcDamages(
                data.api_kouku.GetSecondFleetDamages(),
                data.api_opening_atack.GetFriendDamages(),
                data.api_hougeki3.GetFriendDamages(),
                data.api_raigeki.GetFriendDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),
                data.api_kouku.GetEnemyDamages(),
                data.api_opening_atack.GetEnemyDamages(),
                data.api_hougeki1.GetEnemyDamages(),
                data.api_hougeki2.GetEnemyDamages(),
                data.api_hougeki3.GetEnemyDamages(),
                data.api_raigeki.GetEnemyDamages()
                );

            this.UpdateBattleRank(true);

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy();

            this.provider.UpdateBattleTypes(data);
        }

        public void Update(combined_battle_midnight_battle data)
        {
            this.Name = "連合艦隊 - 夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.PreUpdateBattleRank(true);

            this.SecondFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());

            this.UpdateBattleRank(true);
        }

        public void Update(combined_battle_sp_midnight data)
        {
            this.Name = "連合艦隊 - 開幕夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation, data.api_eSlot);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.PreUpdateBattleRank(true);

            this.SecondFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());

            this.UpdateBattleRank(true);

            this.FriendAirSupremacy = AirSupremacy.航空戦なし;

            this.provider.UpdateBattleTypes(data);
        }

        public void Update(practice_battle data)
        {
            this.Name = "演習 - 昼戦";

            this.UpdateFleets(data.api_dock_id, data.api_ship_ke, data.api_formation, null, false);
            this.Enemies.Name = "";
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.ClearBattleRank();
            this.PreUpdateBattleRank();

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_opening_atack.GetFriendDamages(),
                data.api_hougeki1.GetFriendDamages(),
                data.api_hougeki2.GetFriendDamages(),
                data.api_raigeki.GetFriendDamages()
                );

            this.Enemies.CalcDamages(
                data.api_kouku.GetEnemyDamages(),
                data.api_opening_atack.GetEnemyDamages(),
                data.api_hougeki1.GetEnemyDamages(),
                data.api_hougeki2.GetEnemyDamages(),
                data.api_raigeki.GetEnemyDamages()
                );

            this.UpdateBattleRank();

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy();
        }

        public void Update(practice_midnight_battle data)
        {
            this.Name = "演習 - 夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, null, null, false);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.PreUpdateBattleRank();

            this.FirstFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());

            this.UpdateBattleRank();
        }

        private void Update(sortie_airbattle data)
        {
            this.Name = "航空戦 - 昼戦";

            this.UpdateFleets(data.api_dock_id, data.api_ship_ke, data.api_formation, data.api_eSlot);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.PreUpdateBattleRank();

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_kouku2.GetFirstFleetDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),    //将来的に増える可能性を想定して追加しておく
                data.api_kouku.GetEnemyDamages(),
                data.api_kouku2.GetEnemyDamages()
                );

            this.UpdateBattleRank();

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy(); // 航空戦2回目はスルー

            this.provider.UpdateBattleTypes(data);
        }

        private void Update(sortie_battle data)
        {
            this.Name = "通常 - 昼戦";

            this.UpdateFleets(data.api_dock_id, data.api_ship_ke, data.api_formation, data.api_eSlot);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.PreUpdateBattleRank();

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_opening_atack.GetFriendDamages(),
                data.api_hougeki1.GetFriendDamages(),
                data.api_hougeki2.GetFriendDamages(),
                data.api_raigeki.GetFriendDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),
                data.api_kouku.GetEnemyDamages(),
                data.api_opening_atack.GetEnemyDamages(),
                data.api_hougeki1.GetEnemyDamages(),
                data.api_hougeki2.GetEnemyDamages(),
                data.api_raigeki.GetEnemyDamages()
                );

            this.UpdateBattleRank();

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy();

            this.provider.UpdateBattleTypes(data);
        }

        #endregion

        private void UpdateFleetsByStartNext(map_start_next startNext, string api_deck_id = null)
        {
            this.UpdatedTime = DateTimeOffset.Now;
            this.Name = "下一海域情报";

            this.provider.UpdateMapData(startNext);

            this.BattleSituation = BattleSituation.なし;
            this.FriendAirSupremacy = AirSupremacy.航空戦なし;
            if (this.FirstFleet != null) this.FirstFleet.Formation = Formation.なし;
            this.Enemies = this.provider.GetNextEnemyFleet(startNext);

            if (api_deck_id != null) this.CurrentDeckId = int.Parse(api_deck_id);
            if (this.CurrentDeckId < 1) return;

            this.UpdateFriendFleets(this.CurrentDeckId);

            this.ClearBattleRank();
        }

        private void Update(battle_result result)
        {
            this.provider.UpdateEnemyName(result);
        }

        private void UpdateFleets(
            int api_deck_id,
            int[] api_ship_ke,
            int[] api_formation = null,
            int[][] api_eSlot = null,
            bool isUpdateEnemyData = true)
        {
            this.UpdatedTime = DateTimeOffset.Now;
            this.UpdateFriendFleets(api_deck_id);

            var master = KanColleClient.Current.Master.Ships;
            this.Enemies = new FleetData(
                api_ship_ke.Where(x => x != -1).Select(x => new MastersShipData(master[x])).ToArray(),
                this.Enemies != null ? this.Enemies.Formation : Formation.なし,
                this.Enemies != null ? this.Enemies.Name : "");

            if (api_formation != null)
            {
                this.BattleSituation = (BattleSituation)api_formation[2];
                if (this.FirstFleet != null) this.FirstFleet.Formation = (Formation)api_formation[0];
                if (this.Enemies != null) this.Enemies.Formation = (Formation)api_formation[1];
                if (isUpdateEnemyData) this.provider.UpdateEnemyData(api_ship_ke, api_formation, api_eSlot);
            }

            this.CurrentDeckId = api_deck_id;
        }

        private void UpdateFriendFleets(int deckID)
        {
            var organization = KanColleClient.Current.Homeport.Organization;
            this.FirstFleet = new FleetData(
                organization.Fleets[deckID].Ships.Select(s => new MembersShipData(s)).ToArray(),
                this.FirstFleet != null ? this.FirstFleet.Formation : Formation.なし,
                organization.Fleets[deckID].Name);
            this.SecondFleet = new FleetData(
                organization.Combined && deckID == 1
                    ? organization.Fleets[2].Ships.Select(s => new MembersShipData(s)).ToArray()
                    : new MembersShipData[0],
                this.SecondFleet != null ? this.SecondFleet.Formation : Formation.なし,
                organization.Fleets[2].Name);
        }

        private void UpdateMaxHP(int[] api_maxhps, int[] api_maxhps_combined = null)
        {
            this.FirstFleet.Ships.SetValues(api_maxhps.GetFriendData(), (s, v) => s.MaxHP = v);
            this.Enemies.Ships.SetValues(api_maxhps.GetEnemyData(), (s, v) => s.MaxHP = v);

            if (api_maxhps_combined == null) return;
            this.SecondFleet.Ships.SetValues(api_maxhps_combined.GetFriendData(), (s, v) => s.MaxHP = v);
        }

        private void UpdateNowHP(int[] api_nowhps, int[] api_nowhps_combined = null)
        {
            this.FirstFleet.Ships.SetValues(api_nowhps.GetFriendData(), (s, v) => s.NowHP = v);
            this.Enemies.Ships.SetValues(api_nowhps.GetEnemyData(), (s, v) => s.NowHP = v);

            if (api_nowhps_combined == null) return;
            this.SecondFleet.Ships.SetValues(api_nowhps_combined.GetFriendData(), (s, v) => s.NowHP = v);
        }

        int? hp_sum = null;
        int? enemy_hp_sum = null;

        private void PreUpdateBattleRank(bool combined = false)
        {
            if (!hp_sum.HasValue)
            {
                hp_sum = this.FirstFleet.Ships.Sum(ship => ship.NowHP);
                if (combined)
                {
                    hp_sum += this.SecondFleet.Ships.Sum(ship => ship.NowHP);
                }
                enemy_hp_sum = this.Enemies.Ships.Sum(ship => ship.NowHP);
            }
        }

        private void UpdateBattleRank(bool combined = false)
        {
            int HPnow = this.FirstFleet.Ships.Sum(ship => ship.NowHP < 0 ? 0 : ship.NowHP);
            double HPsum = hp_sum ?? this.FirstFleet.Ships.Sum(ship => ship.MaxHP);
            int EHPnow = this.Enemies.Ships.Sum(ship => ship.NowHP < 0 ? 0 : ship.NowHP);
            double EHPsum = enemy_hp_sum ?? this.Enemies.Ships.Sum(ship => ship.MaxHP);
            if (combined)
            {
                HPnow += this.SecondFleet.Ships.Sum(ship => ship.NowHP < 0 ? 0 : ship.NowHP);
                if (!hp_sum.HasValue)
                {
                    HPsum += this.SecondFleet.Ships.Sum(ship => ship.MaxHP);
                }
            }
            double HPlose = (HPsum - HPnow) / HPsum;
            double EHPlose = (EHPsum - EHPnow) / EHPsum;
            int EShipCount = this.Enemies.Ships.Count();
            int ESunkCount = this.Enemies.Ships.Count(ship => ship.NowHP <= 0);
            ShipData EFlagShip = this.Enemies.Ships.First();
            if (ESunkCount == EShipCount)
            {
                if (HPlose != 0)
                    this.RankPrediction = BattleRank.S;
                else
                    this.RankPrediction = BattleRank.SS;
            }
            else if (ESunkCount > (EShipCount - (EShipCount <= 4 ? 1 : 0)) / 2)
            {
                this.RankPrediction = BattleRank.A;
            }
            else if (EFlagShip != null && EFlagShip.NowHP <= 0)
            {
                this.RankPrediction = BattleRank.B;
            }
            else if (EHPlose > 2.5 * HPlose)
            {
                this.RankPrediction = BattleRank.B;
            }
            else if (EHPlose <= 0.05)
            {
                this.RankPrediction = BattleRank.D;
            }
            else
            {
                this.RankPrediction = BattleRank.C;
            }
        }

        private void ClearBattleRank()
        {
            hp_sum = null;
            enemy_hp_sum = null;
            this.RankPrediction = null;
        }
    }
}
