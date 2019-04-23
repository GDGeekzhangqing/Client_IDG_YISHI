﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDG;

public class SkillCheck{
    PlayerData player;
    SkillNode node;
    List<NetData> others;
	public SkillCheck Check(PlayerData player,Fixed width,Fixed height,Fixed forwardOffset,SkillNode node){
        this.node=node;
        this.player=player;
		var shap= new BoxShap(height,width,player.transform.forward*forwardOffset+player.transform.Position,player.transform.Rotation);
        var otherList= player.client.physics.OverlapShap(shap);
        others=otherList;
        if(otherList.Count>0){
           // UnityEngine.Debug.LogError("碰撞数 "+otherList.Count  );
            foreach (var other in otherList)
            {
                if(other is NetData){
            //        UnityEngine.Debug.LogError("碰撞 "+other.name+" "+other.GetType()   );
                }
            }
            RunNodes(node.GetNodes(SkillTrigger.Check));
        }
        
		
		return this;
	}

     public void RunNodes(List<SkillNode> nodes){
        foreach (var node in nodes)
        {
            RunNode(node);
        }
    }
    public void RunNode(SkillNode node){
       
       // UnityEngine.Debug.Log("runNode ["+node.trigger+"] "+node.type);
        switch (node.type)
        {
            case SkillNodeType.Damage:
                foreach (var other in others)
                {
                    if(other is HealthData){
                        if(other!=player){
                            var enemy=other as HealthData;
                            enemy.GetHurt(node.intParams[0]/10f.ToFixed());
                        }
                    }
                }
            break;
            case SkillNodeType.CreatCheck:
            
                new SkillCheck().Check(player,node.intParams[0]/10f.ToFixed(),node.intParams[1]/10f.ToFixed(),node.intParams[2]/10f.ToFixed(),node);
            break;
            default:break;
        }
    }

}
public enum SkillStatus
{
    CanUse,
    UseStart,
    UseOver,
    CoolDown,
}
public class SkillBase : ComponentBase
{
    public SkillId skillId;

    public SkillStatus status = SkillStatus.CanUse;
    public KeyNum key
    {
        get
        {
            return skillData.key;
        }
    }
    public Fixed timer;

    public PlayerData player = null;
    public int skillSetId;
    public SkillData skillData;

    public CoroutineManager coroutine
    {
        get
        {
            return data.client.coroutine;
        }
    }

    public void StartUse()
    {
        RunNodes(skillData.GetNodes(SkillTrigger.PressStart));

    }
    public void UseOver()
    {


        RunNodes(skillData.GetNodes(SkillTrigger.PressOver));

        if (player.CanMove)
        {
            UnityEngine.Debug.LogError("错误UseOver " + status);
        }
        //          UnityEngine.Debug.LogError("useOver");
    }
    public void StayUse()
    {
        RunNodes(skillData.GetNodes(SkillTrigger.PressStay));
        //        UnityEngine.Debug.LogError("stayUse");
    }

    public void AnimOver()
    {

        RunNodes(skillData.GetNodes(SkillTrigger.AnimOver));
    }


    public void CoolDown()
    {
       
    }
    public override void Update()
    {
        if (data.Input.GetKeyDown(key))
        {
             //    UnityEngine.Debug.LogError("update " + data.client.inputCenter.Time +" "+data.view.gameObject.name);
            UnityEngine.Debug.LogError("keyDown " + key + " " + data.view.gameObject.name);
        }
        if (data.Input.GetKeyUp(key))
        {
            UnityEngine.Debug.LogError("keyUp " + key + " " + data.view.gameObject.name);     
        }
        if (status == SkillStatus.CanUse)
        {
            if (data.Input.GetKeyDown(key))
            {

                StartUse();
                status = SkillStatus.UseStart;
            }
           
        }
        if (status == SkillStatus.UseStart)
        {
            if (data.Input.GetKey(key))
            {
                // UnityEngine.Debug.LogError("GetKey使用技能 ["+skillId+"] ");
                StayUse();

            }
            if (data.Input.GetKeyUp(key))
            {

                UseOver();
                // UnityEngine.Debug.LogError("GetKeyUp使用技能 ["+skillId+"] ");
                (data as PlayerData).SetAnimTrigger("UseSkill");
                timer = skillData.animTime;
                status = SkillStatus.UseOver;
            }
        }
         if (status == SkillStatus.UseOver)
        {
            if (timer > 0)
            {
                timer -= data.deltaTime;
            }
            else
            {
                timer = skillData.coolDownTime;
                AnimOver();
                status = SkillStatus.CoolDown;

            }
        }
        else if (status == SkillStatus.CoolDown)
        {
            if (timer > 0)
            {
                timer -= data.deltaTime;
            }
            else
            {
                status = SkillStatus.CanUse;
            }
        }
        //  CoolDown();

    }

    public void RunNodes(List<SkillNode> nodes)
    {
        foreach (var node in nodes)
        {
            RunNode(node);
        }
    }
    public void RunNode(SkillNode node)
    {
        if (player == null)
        {
            player = data as PlayerData;
        }
        // UnityEngine.Debug.Log("runNode ["+node.trigger+"] "+node.type);
        switch (node.type)
        {
            case SkillNodeType.RotatePlayer:
                if (player != null)
                {
                    var rot = data.Input.GetJoyStickDirection(key);
                    if (rot.magnitude < 0.1f)
                    {
                        rot = data.transform.forward;
                    }
                    player.transform.Rotation = rot.ToRotation();
                }
                break;
            case SkillNodeType.MoveCtr:
                if (node.boolParams[0])
                {
                    player.CanMove = true;
                    player.animRootMotion(false);
                }
                else
                {
                    player.CanMove = false;
                    player.animRootMotion(true);
                }
                break;
            case SkillNodeType.WaitTime:

                coroutine.WaitCall(node.intParams[0] / 10f.ToFixed(),
                    () =>
                    {
                        RunNodes(node.GetNodes(SkillTrigger.Time));
                    });
                break;
            case SkillNodeType.CreatCheck:

                new SkillCheck().Check(player, node.intParams[0] / 10f.ToFixed(), node.intParams[1] / 10f.ToFixed(), node.intParams[2] / 10f.ToFixed(), node);
                break;
            default: break;
        }
    }
} 

//public class SkillSet:ComponentBase{
//    public List<SkillBase> skills;
//    public int currentId;
//    public SkillBase GetCurrentSkill(){
//        if(currentId>=0){
//            return skills[currentId];
//        }else
//        {
//            return null;
//        }
//    }
//    public override void Init(){
//        skills=new List<SkillBase>();
//        currentId=-1;

//    }
//    public int NextId(){
//        currentId++;
//        currentId=currentId%skills.Count;
//        if(data==null){
//            UnityEngine.Debug.LogError("data is null");
//        }
//        var func= (data as PlayerData).skillList.changeSkill;
//        if(func!=null){
//            func(skills[currentId].skillId);
//        }
//        return currentId;
//    }
//    public void Add(SkillBase skill){
//        this.skills.Clear();
//        skill.skillSetId=this.skills.Count;
//        this.skills.Add(skill);
//        skill.set=this;
//        if(currentId==-1){
//            NextId();
//        }
         
//    }
//    public override void Update(){
        
//        var curSkill=GetCurrentSkill();
//        if(curSkill!=null){
//            curSkill.Update();
//        }
//        foreach (var skill in skills)
//        {
//            skill.CoolDown();

//        }
//    }
//}
public class SkillAction:ComponentBase
{
    public Dictionary<KeyNum, SkillBase> skillTable;
    public Action<SkillId> changeSkill;
    public override void Init()
    {
        skillTable = new Dictionary<KeyNum, SkillBase>();
        //skillTable.Add(KeyNum.Skill1, null);
        //skillTable.Add(KeyNum.Skill2, null);
        //skillTable.Add(KeyNum.Skill3, null);
    }
    public SkillBase GetSkill(KeyNum key){
        SkillBase skill=null;
        if (skillTable.ContainsKey(key)){
            skill = skillTable[key];
        }
        return skill;
    }
    public void AddSkill(SkillId skillId){
        if(skillId==SkillId.none)return;
        AddSkill(SkillManager.GetSkill(skillId));
       
    }
    public void AddSkill(SkillBase skill)
    {
        skill.InitNetData(this.data);
     
        if (skillTable.ContainsKey(skill.key))
        {
            skillTable[skill.key] = skill;
        }else
        {
            skillTable.Add(skill.key, skill);
            //UnityEngine.Debug.LogError("错误的Key "+skill.key);
        }
        changeSkill(skill.skillId);
        //
    }
    public override void Update()
    {
   
        foreach (var item in skillTable)
        {
            // if (item.Value.Count>0)
            // {
            //     item.Value[item.Value.Count-1].Update();
            // }

            item.Value.Update();
         //   UnityEngine.Debug.LogError(item.Key+" "+item.Value.skills.Count);
        } 
    }

}

