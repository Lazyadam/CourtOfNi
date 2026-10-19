using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourtOfNi.Core.Missions;
using CourtOfNi.Data;
using CourtOfNi.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CourtOfNi.EditorTools
{
    /// <summary>
    /// 메뉴 한 번으로 테스트용 씬과 샘플 데이터(증거물 카드, 배심원 미션, 규칙 에셋)를 만든다.
    /// Court of Ni ▸ Create Dev Scene and Sample Data
    /// </summary>
    public static class DevSceneBuilder
    {
        const string DataRoot = "Assets/_Project/Data/Sample";
        const string ScenePath = "Assets/_Project/Scenes/Dev.unity";

        static readonly (string id, string title, string description)[] SampleEvidence =
        {
            ("umbrella", "젖은 우산", "비가 오지 않은 날, 현관에 놓여 있던 젖은 우산."),
            ("cake", "반쯤 먹은 케이크", "포크 자국이 두 종류다."),
            ("flowerpot", "깨진 화분", "창가에서 떨어진 것 같다."),
            ("pawprint", "고양이 발자국", "부엌에서 거실까지 이어진다."),
            ("deleted-text", "지워진 문자", "\"오늘 밤 거기서\"까지만 보인다."),
            ("receipt", "수상한 영수증", "새벽 2시 47분, 편의점, 고무장갑 1개."),
            ("sock", "한 짝뿐인 양말", "나머지 한 짝은 어디에?"),
            ("key", "잃어버린 열쇠", "누구의 열쇠인지 아무도 모른다."),
            ("voicemail", "음성 메시지", "웃음소리와 함께 끊겼다."),
            ("photo", "찢어진 사진", "두 사람 중 한 명의 얼굴만 남아 있다."),
            ("cctv", "새벽 3시 CCTV", "후드티를 입은 누군가가 지나간다."),
            ("wine", "빈 와인병", "라벨에 립스틱 자국이 있다."),
        };

        [MenuItem("Court of Ni/Create Dev Scene and Sample Data")]
        public static void CreateDevSceneAndSampleData()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var ruleset = CreateSampleData();
            CreateScene(ruleset);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Court of Ni",
                $"샘플 데이터({DataRoot})와 테스트 씬({ScenePath})을 만들었습니다.\n" +
                "Play 후 '호스트'를 누르고, 다른 클라이언트(Multiplayer Play Mode 또는 빌드)에서 '접속'하세요.", "확인");
        }

        static GameRulesetAsset CreateSampleData()
        {
            EnsureFolder($"{DataRoot}/Evidence");
            EnsureFolder($"{DataRoot}/Missions");

            var cards = SampleEvidence.Select(e => CreateOrLoad<EvidenceCardAsset>($"{DataRoot}/Evidence/Evidence_{e.id}.asset", so =>
            {
                so.FindProperty("id").stringValue = e.id;
                so.FindProperty("title").stringValue = e.title;
                so.FindProperty("description").stringValue = e.description;
            })).ToList();

            var missions = new List<JuryMissionAsset>
            {
                CreateOrLoad<VoteOnlyForSideMissionAsset>($"{DataRoot}/Missions/Mission_InnocentOnly.asset", so =>
                {
                    Common(so, "innocent-only", "모든 라운드에서 무고한 피고인에게만 투표하면 +1점, 실패하면 -1점", 1, 1);
                    so.FindProperty("side").enumValueIndex = (int)VerdictSide.Innocent;
                }),
                CreateOrLoad<VoteOnlyForSideMissionAsset>($"{DataRoot}/Missions/Mission_ConvictedOnly.asset", so =>
                {
                    Common(so, "convicted-only", "모든 라운드에서 범인으로 판결된 피고인에게만 투표하면 +1점", 1, 0);
                    so.FindProperty("side").enumValueIndex = (int)VerdictSide.Convicted;
                }),
                CreateOrLoad<DefendantAttributeMissionAsset>($"{DataRoot}/Missions/Mission_OldestInnocent.asset", so =>
                {
                    Common(so, "oldest-innocent", "피고인 중 나이가 더 많은 사람이 무죄가 되면 +1점", 1, 0);
                    so.FindProperty("attributeKey").stringValue = "age";
                    so.FindProperty("highest").boolValue = true;
                    so.FindProperty("side").enumValueIndex = (int)VerdictSide.Innocent;
                }),
                CreateOrLoad<DefendantAttributeMissionAsset>($"{DataRoot}/Missions/Mission_YoungestConvicted.asset", so =>
                {
                    Common(so, "youngest-convicted", "피고인 중 나이가 더 어린 사람이 유죄가 되면 +1점", 1, 0);
                    so.FindProperty("attributeKey").stringValue = "age";
                    so.FindProperty("highest").boolValue = false;
                    so.FindProperty("side").enumValueIndex = (int)VerdictSide.Convicted;
                }),
            };

            return CreateOrLoad<GameRulesetAsset>($"{DataRoot}/Ruleset_Sample.asset", so =>
            {
                SetList(so.FindProperty("evidenceCards"), cards);
                SetList(so.FindProperty("juryMissions"), missions);
            });
        }

        static void CreateScene(GameRulesetAsset ruleset)
        {
            EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var networkManagerGo = new GameObject("NetworkManager");
            var networkManager = networkManagerGo.AddComponent<NetworkManager>();
            var transport = networkManagerGo.AddComponent<UnityTransport>();
            if (networkManager.NetworkConfig == null) networkManager.NetworkConfig = new NetworkConfig();
            networkManager.NetworkConfig.NetworkTransport = transport;

            var gameGo = new GameObject("CourtGame");
            gameGo.AddComponent<NetworkObject>();
            var game = gameGo.AddComponent<CourtNetworkGame>();
            var gameSo = new SerializedObject(game);
            gameSo.FindProperty("ruleset").objectReferenceValue = ruleset;
            gameSo.ApplyModifiedPropertiesWithoutUndo();

            var uiGo = new GameObject("DevUI");
            uiGo.AddComponent<DevLobbyGUI>();
            uiGo.AddComponent<VoiceRouter>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (buildScenes.All(s => s.path != ScenePath))
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }
        }

        static void Common(SerializedObject so, string id, string description, int reward, int penalty)
        {
            so.FindProperty("id").stringValue = id;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("reward").intValue = reward;
            so.FindProperty("penalty").intValue = penalty;
        }

        static T CreateOrLoad<T>(string path, System.Action<SerializedObject> fill) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            fill(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void SetList<T>(SerializedProperty list, IReadOnlyList<T> items) where T : Object
        {
            list.arraySize = items.Count;
            for (var i = 0; i < items.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
