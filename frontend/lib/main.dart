import 'package:flutter/material.dart';
import 'dart:convert';
import 'dart:io' show Platform;
import 'package:http/http.dart' as http;
import 'package:flutter/rendering.dart';
import 'package:flutter/foundation.dart';

void main() {
  // Flutterエンジンを初期化
  WidgetsFlutterBinding.ensureInitialized();
  
  // Linux向けのレンダリング設定を追加
  debugPaintSizeEnabled = false;
  
  // Linuxプラットフォーム固有の設定
  if (!kIsWeb && Platform.isLinux) {
    // Linuxでのレンダリング設定
    debugPrint('Running on Linux platform');
  }
  
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'クイズアプリ',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue),
        useMaterial3: true,
      ),
      home: const QuizPage(),
    );
  }
}

// 問題データを表すモデルクラス
class Question {
  final String id;
  final String text;

  Question({required this.id, required this.text});

  factory Question.fromJson(Map<String, dynamic> json) {
    return Question(
      id: json['id'],
      text: json['text'],
    );
  }
}

// 問題の状態を管理するための列挙型
enum QuestionState {
  loading,
  success,
  failure,
}

class QuizPage extends StatefulWidget {
  const QuizPage({super.key});

  @override
  State<QuizPage> createState() => _QuizPageState();
}

class _QuizPageState extends State<QuizPage> {
  QuestionState _questionState = QuestionState.loading;
  Question? _question;
  String? _errorMessage;
  bool? _isCorrect; // 回答が正解だったか
  String? _correctAnswer; // 正しい解答
  String? _explanation; // 解説


  @override
  void initState() {
    super.initState();
    _fetchQuestion();
  }

  // 問題を取得するHTTPリクエスト
  Future<void> _fetchQuestion() async {
    setState(() {
      _questionState = QuestionState.loading;
    });

    try {
      final response = await http.get(Uri.parse('http://localhost:5067/exam'));
      
      if (response.statusCode == 200) {
        final questionData = jsonDecode(response.body);
        setState(() {
          _question = Question.fromJson(questionData);
          _questionState = QuestionState.success;
        });
      } else {
        setState(() {
          _errorMessage = '問題の取得に失敗しました: ステータスコード ${response.statusCode}';
          _questionState = QuestionState.failure;
        });
      }
    } catch (e) {
      setState(() {
        _errorMessage = '問題の取得に失敗しました: $e';
        _questionState = QuestionState.failure;
      });
    }
  }

  // 回答を送信するHTTPリクエスト
  // 回答を送信するHTTPリクエスト
  Future<void> _submitAnswer(String questionId, bool isCorrect) async {
    setState(() {
      _questionState = QuestionState.loading; // 回答送信中もローディング表示
      _isCorrect = null; // 結果をリセット
      _correctAnswer = null;
      _explanation = null;
    });

    try {
      final response = await http.post(
        Uri.parse('http://localhost:5067/answer'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'questionId': questionId,
          'isCorrect': isCorrect, // boolean値を送信
        }),
      );
      debugPrint('Response status code: ${response.statusCode}');

      if (response.statusCode == 200) {
        final answerResult = jsonDecode(response.body);
        setState(() {
          _isCorrect = answerResult['isCorrect'];
          _correctAnswer = answerResult['correctAnswer'];
          _explanation = answerResult['explanation'];
          _questionState = QuestionState.success; // 結果表示のためにsuccess状態を維持
        });
      } else {
        setState(() {
          _errorMessage = '回答の送信に失敗しました: ステータスコード ${response.statusCode}';
          _questionState = QuestionState.failure;
        });
      }
    } catch (e) {
      setState(() {
        _errorMessage = '回答の送信に失敗しました: $e';
        _questionState = QuestionState.failure;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        title: const Text('クイズアプリ'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(30.0),
        child: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: _buildContent(),
          ),
        ),
      ),
    );
  }

  List<Widget> _buildContent() {
    switch (_questionState) {
      case QuestionState.loading:
        return [
          const CircularProgressIndicator(),
          const SizedBox(height: 20),
          const Text('処理中...'), // 問題読み込み中と回答送信中の両方で使用
        ];

      case QuestionState.success:
        // 問題表示と回答入力、または回答結果表示
        List<Widget> content = [
          Text(
            _question!.text,
            style: const TextStyle(fontSize: 18),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 20),
        ];

        if (_isCorrect == null) {
          // 回答選択ボタン
          content.addAll([
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              children: [
                ElevatedButton(
                  onPressed: () => _submitAnswer(_question!.id, true),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 30, vertical: 15),
                  ),
                  child: const Text('正しい'),
                ),
                ElevatedButton(
                  onPressed: () => _submitAnswer(_question!.id, false),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 30, vertical: 15),
                  ),
                  child: const Text('間違い'),
                ),
              ],
            ),
          ]);
        } else {
          // 回答結果表示
          content.addAll([
            Text(
              _isCorrect! ? '正解！' : '不正解...',
              style: TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.bold,
                color: _isCorrect! ? Colors.green : Colors.red,
              ),
            ),
            const SizedBox(height: 10),
            Text('正しい解答: $_correctAnswer'),
            const SizedBox(height: 10),
            Text('解説: $_explanation'),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: () {
                _fetchQuestion(); // 次の問題を取得
              },
              child: const Text('次の問題へ'),
            ),
          ]);
        }
        return content;

      case QuestionState.failure:
        return [
          Icon(Icons.error, color: Colors.red[700], size: 60),
          const SizedBox(height: 20),
          Text(
            _errorMessage ?? '問題の読み込みに失敗しました',
            style: TextStyle(color: Colors.red[700]),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 30),
          ElevatedButton(
            onPressed: _fetchQuestion,
            child: const Text('再試行'),
          ),
        ];
    }
  }
}
