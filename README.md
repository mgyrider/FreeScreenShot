
# 概要
本ソフトウェアは、PC上でスクリーンショットを取得し、選択範囲の角度を360度自由に回転できるWPFベースのデスクトップアプリケーションです。直感的な操作でスクリーンショットの編集を行うことができます。

例えば、下記のウェブページに斜めな図表があるとします、この図表をまっすぐに表示した状態でスクリーンショットするには、どうすればよいでしょうか？

本ソフトウェアを使えば、不要な部分を含めずに、図表だけをぴったり選択してスクリーンショットできます。

<img src="https://raw.githubusercontent.com/mgyrider/mgyrider.github.io/refs/heads/master/images/screenshotApp1.png" width="80%">

結果は下記のように表示されます、クリップボードにコピーしたり、PNGファイルとして保存したりすることが可能です。

<img src="https://raw.githubusercontent.com/mgyrider/mgyrider.github.io/refs/heads/master/images/screenshotApp2.png" width="50%">

</br>

# 特徴
- スクリーンショットの取得（マルチスクリーン対応）

- 選択範囲の編集（サイズ調整・360度回転）

- 画像の保存（PNG形式）及びクリップボードコピー

</br>

# 動作環境
- 対応OS: Windows 10 / 11
- 開発環境: .NET 8 以上 / C#（WPF）

</br>

## ビルド
プロジェクトのフォルダーで、以下のコマンドを実行します。

```dotnet run```