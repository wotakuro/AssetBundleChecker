# このツールについて
このツールは AssetBundleの中身を確認する用のツールになっています。<br />
<br />
今のところ、下記の機能を持っています。<br />
・AssetBundle内にどんなPrefabやアセットが入っているか確認する機能<br />
・AssetBundleに含まれるShaderのVariantをダンプする機能<br />


# 使用方法
## 呼び出しについて
「Tools/UTJ/AssetBundleChecker」で下記ウィンドウを出します。<br />
![Alt text](/Documentation~/AssetBundleChecker.png) <br />
1.アセットバンドルのファイルを指定して読み込むボタンです。<br />
　読み込んだ時にPrefabがあればInstanciateしてSceneViewに出します<br />
2.読み込んだアセットバンドルをクリアします<br />
<br />
<br />
## AssetBundleに含まれるものを確認する
![Alt text](/Documentation~/AssetBundleMode.png) <br />

Load AssetBundlesタブを選ぶと、下記のように読み込んだAssetBundle一覧を出します<br />
1.読み込んだアセットバンドルをFoldメニューで出します。右側の×でアンロードします。<br />
2.PrefabからInstanciateしたGameObjectのShaderをプロジェクトにあるShaderを使うか、アセットバンドルにあるものを使うか確認します<br />

## ShaderVariantを確認する
![Alt text](/Documentation~/AssetBundleShader.png) <br />

AssetBunle Shadersにすると、AssetBundle中のShaderをリストに出します<br />
Shaderを開いて 「Dump Shader」を押すと、プロジェクトの直下に シェーダーの情報をjsonにして書き出します。<br />
<br />
![Alt text](/Documentation~/AssetBundleShaderJson.png) <br />

# TODO…
UI ElementsにしてUIの一新。<br />
DumpShader一括対応<br />
Manufestファイルを使った依存解決…<br />

